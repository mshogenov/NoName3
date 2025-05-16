using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MakeBreak.Filters;
using MakeBreak.Models;


namespace MakeBreak.Services;

public class MakeBreakServices
{
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private readonly Document _doc = Context.ActiveDocument;

    public void CreateTwoCouplingsAndSetMidPipeParameter(FamilySymbol familySymbol)
    {
        while (true)
        {
            var selectReference1 = SelectReference("Выберите первую точку на трубе", new SelectionFilter());
            // Проверяем, нашли ли мы трубу
            if (selectReference1 == null) return;
            Break break1 = new Break(selectReference1, _doc);
            using TransactionGroup tg = new TransactionGroup(_doc, "Сделать разрыв");
            try
            {
                tg.Start();
                using Transaction transaction = new Transaction(_doc, "Вставка первой муфты");
                transaction.Start();
                var firstBreak = InsertBreak(familySymbol, break1);
                if (firstBreak != null)
                {
                    var firstSplitPipe = firstBreak.Value.SplitPipe;
                    var firstCoupling = firstBreak.Value.Coupling;
                    if (firstSplitPipe == null || firstCoupling == null)
                    {
                        TaskDialog.Show("Ошибка", "Не удалось создать муфту в первой точке.");
                        transaction.RollBack();
                        tg.RollBack();
                        return;
                    }

                    var displacementCreate1 = DisplacementCreate(break1, firstSplitPipe, firstCoupling);
                    transaction.Commit();

                    Reference selectReference2 =
                        SelectReference("Выберите вторую точку на трубе", new SelectionFilter());
                    if (selectReference2 == null)
                    {
                        tg.RollBack();
                        return;
                    }

                    Break break2 = new Break(selectReference2, _doc);

                    // Проверяем минимальное расстояние между точками
                    double distanceBetweenPoints = break1.BreakPoint.DistanceTo(break2.BreakPoint).ToMillimeters();
                    const double minimumDistance = 20; //мм
                    if (distanceBetweenPoints < minimumDistance)
                    {
                        TaskDialog.Show("Предупреждение",
                            $"Выбранные точки расположены слишком близко друг к другу (расстояние: {distanceBetweenPoints} миллиметров). " +
                            $"Минимальное допустимое расстояние: {minimumDistance} миллиметров. " +
                            "Операция отменена.");
                        tg.RollBack();
                        return;
                    }

                    // Определяем, какую трубу разрезать для второй точки
                    var secondPipeToCut = GetPipeToCut(break1, firstSplitPipe, break2);
                    using Transaction trans2 = new Transaction(_doc, "Вставка второй муфты");
                    trans2.Start();
                    var secondBreak = InsertBreak(familySymbol, break2);
                    if (secondBreak == null)
                    {
                        TaskDialog.Show("Предупреждение", "Не удалось создать муфту во второй точке.");
                        trans2.RollBack();
                        tg.RollBack();
                        return;
                    }

                    var secondSplitPipe = secondBreak.Value.SplitPipe;
                    // Создаем муфту между разрезанными частями (используя "Разрыв")
                    var secondCoupling = secondBreak.Value.Coupling;
                    if (secondCoupling == null || secondSplitPipe == null)
                    {
                        TaskDialog.Show("Предупреждение", "Не удалось создать муфту во второй точке.");
                        trans2.RollBack();
                        tg.RollBack();
                        return;
                    }

                    // Определяем среднюю трубу между двумя точками разреза
                    var midPipe = GetMidPipe(secondPipeToCut, break1, secondSplitPipe, break2, firstBreak,
                        firstSplitPipe);
                    SetParameterBreak(midPipe.Pipe);
                    trans2.Commit();
                    Transaction tr = new Transaction(_doc, "dfdf");
                    tr.Start();
                    CreateDisplacementElement(break1, secondSplitPipe, midPipe, firstSplitPipe, secondCoupling,
                        displacementCreate1);
                    tr.Commit();
                }

                tg.Assimilate();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
        }
    }

    private void CreateDisplacementElement(Break break1, PipeWrapper secondSplitPipe, PipeWrapper midPipe,
        PipeWrapper firstSplitPipe, FamilyInstance secondCoupling, DisplacementElement displacementCreate1)
    {
        DisplacementElement displacementCreate2 = null;
        if (break1.PrimaryDisplacement == null) return;
        // Проверяем, можно ли смещать элементы и не смещены ли они уже
        List<ElementId> validElementIds = [];
        if (DisplacementElement.IsAllowedAsDisplacedElement(break1.TargetPipe.Pipe) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, break1.TargetPipe.Id))
        {
            validElementIds.Add(break1.TargetPipe.Id);
        }

        if (DisplacementElement.IsAllowedAsDisplacedElement(secondSplitPipe.Pipe) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, secondSplitPipe.Id))
        {
            validElementIds.Add(secondSplitPipe.Id);
        }

        // Проверяем midPipe
        if (DisplacementElement.IsAllowedAsDisplacedElement(midPipe.Pipe) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, midPipe.Id))
        {
            validElementIds.Add(midPipe.Id);
        }

        if (DisplacementElement.IsAllowedAsDisplacedElement(firstSplitPipe.Pipe) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, firstSplitPipe.Id))
        {
            validElementIds.Add(firstSplitPipe.Id);
        }

        // Проверяем secondCoupling
        if (DisplacementElement.IsAllowedAsDisplacedElement(secondCoupling) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, secondCoupling.Id))
        {
            validElementIds.Add(secondCoupling.Id);
        }

        // Создаем смещение только если есть валидные элементы
        if (validElementIds.Count > 0)
        {
            displacementCreate2 = DisplacementElement.Create(
                _doc,
                validElementIds,
                new XYZ(),
                Context.ActiveView,
                break1.PrimaryDisplacement);
        }
        else
        {
            // Выводим сообщение, что элементы не могут быть смещены
            TaskDialog.Show("Ошибка", "Выбранные элементы не могут быть смещены или уже смещены.");
        }

        MergeDisplacementElements(_doc, new List<DisplacementElement>()
        {
            displacementCreate2,
            displacementCreate1,
            break1.PrimaryDisplacement
        }, break1.PrimaryDisplacement);
    }

    private PipeWrapper GetMidPipe(PipeWrapper secondPipeToCut, Break break1, PipeWrapper secondSplitPipe, Break break2,
        (PipeWrapper SplitPipe, FamilyInstance Coupling)? firstBreak, PipeWrapper firstSplitPipe)
    {
        PipeWrapper midPipe = null;
        if (secondPipeToCut != null && break1.TargetPipe != null && secondPipeToCut.Id.Equals(break1.TargetPipe.Id) ||
            secondPipeToCut != null && firstBreak != null &&
            secondPipeToCut.Id.Equals(firstSplitPipe.Id))
        {
            midPipe = new PipeWrapper(DetermineMidPipeByDistance(secondPipeToCut, secondSplitPipe, break1.BreakPoint,
                break2.BreakPoint));
        }

        return midPipe;
    }

    private static PipeWrapper GetPipeToCut(Break break1, PipeWrapper firstSplitPipe,
        Break break2)
    {
        PipeWrapper pipeToCut;

        // Проверяем, какая из труб после разрезания имеет такой же ElementId
        if (break1.TargetPipe?.Id.Value == break2.TargetPipe.Id.Value)
        {
            pipeToCut = break1.TargetPipe;
        }
        else if (firstSplitPipe?.Id.Value == break2.TargetPipe.Id.Value)
        {
            pipeToCut = firstSplitPipe;
        }
        else
        {
            // Используем запасной вариант - проверка по расстоянию

            if (break1.TargetPipe == null) return null;
            double dist1 = break1.TargetPipe.Curve.Distance(break2.BreakPoint);
            if (firstSplitPipe == null) return null;
            double dist2 = firstSplitPipe.Curve.Distance(break2.BreakPoint);
            ;
            pipeToCut = dist1 < dist2 ? break1.TargetPipe : firstSplitPipe;
        }

        return pipeToCut;
    }

    private DisplacementElement DisplacementCreate(Break break1, PipeWrapper firstSplitPipe,
        FamilyInstance firstCoupling)
    {
        DisplacementElement displacementCreate1 = null;
        if (break1.SelectedElement is not DisplacementElement displacement1) return null;
        // Проверяем, можно ли смещать элементы и не смещены ли они уже
        List<ElementId> validElementIds = [];

        if (DisplacementElement.IsAllowedAsDisplacedElement(break1.TargetPipe.Pipe) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, break1.TargetPipe.Id))
        {
            validElementIds.Add(break1.TargetPipe.Id);
        }

        if (DisplacementElement.IsAllowedAsDisplacedElement(firstSplitPipe.Pipe) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, firstSplitPipe.Id))
        {
            validElementIds.Add(firstSplitPipe.Id);
        }

        // Проверяем secondCoupling
        if (DisplacementElement.IsAllowedAsDisplacedElement(firstCoupling) &&
            !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, firstCoupling.Id))
        {
            validElementIds.Add(firstCoupling.Id);
        }

        // Создаем смещение только если есть валидные элементы
        if (validElementIds.Count > 0)
        {
            displacementCreate1 = DisplacementElement.Create(
                _doc,
                validElementIds,
                new XYZ(),
                Context.ActiveView,
                displacement1);
        }
        else
        {
            // Выводим сообщение, что элементы не могут быть смещены
            TaskDialog.Show("Ошибка", "Выбранные элементы не могут быть смещены или уже смещены.");
        }

        return displacementCreate1;
    }

    private (PipeWrapper SplitPipe, FamilyInstance Coupling)? InsertBreak(FamilySymbol familySymbol, Break break1)
    {
        ElementId firstSplitPipeId = PlumbingUtils.BreakCurve(_doc, break1.TargetPipe.Id, break1.BreakPoint);
        // Разрезаем трубу в первой точке

        if (firstSplitPipeId == ElementId.InvalidElementId)
        {
            return null;
        }

        PipeWrapper firstSplitPipe =
            new PipeWrapper(_doc.GetElement(firstSplitPipeId) as Pipe); // Вторая часть - новая труба
        // Создаем муфту между первой и второй частью (используя "Разрыв")
        var firstCoupling = CreateCouplingBetweenPipes(break1, firstSplitPipe, familySymbol);
        if (firstCoupling == null)
        {
            return null;
        }

        return (firstSplitPipe, firstCoupling);
    }

    private void SetParameterBreak(Pipe pipe)
    {
        var activeView = pipe.Document.ActiveView;
        switch (activeView.ViewType)
        {
            case ViewType.ThreeD:
            {
                Parameter commentParam = pipe?.FindParameter("msh_Разрыв");
                commentParam?.Set(true);
                break;
            }
            case ViewType.FloorPlan:
            {
                Parameter commentParam = pipe?.FindParameter("msh_Разрыв_План");
                commentParam?.Set(true);
                break;
            }
        }
    }

    private Reference SelectReference(string statusPrompt, ISelectionFilter selectionFilter)
    {
        try
        {
            Reference reference = _uidoc.Selection.PickObject(ObjectType.Element, selectionFilter,
                statusPrompt);
            return reference;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            TaskDialog.Show("Ошибка", "Произошла ошибка: " + e.Message);
        }

        return null;
    }

    private FamilyInstance CreateCouplingBetweenPipes(Break bBreak, PipeWrapper splitPipe, FamilySymbol familySymbol)
    {
        try
        {
            // Получаем незанятые коннекторы труб
            Connector conn1 = NearestConnector(bBreak.TargetPipe.AllConnectors, bBreak.BreakPoint);
            Connector conn2 = NearestConnector(splitPipe.AllConnectors, bBreak.BreakPoint);
            if (conn1 == null || conn2 == null)
            {
                TaskDialog.Show("Ошибка", "Не удалось найти открытые коннекторы на трубах.");
                return null;
            }

            if (familySymbol != null)
            {
                // Активируем символ семейства, если он не активен
                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                }

                // Создаем экземпляр фитинга
                FamilyInstance newFamilyInstance = _doc.Create.NewFamilyInstance(
                    bBreak.BreakPoint,
                    familySymbol,
                    bBreak.TargetPipe.ReferenceLevel,
                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                SetDiameterFitting(bBreak.TargetPipe, newFamilyInstance);

                // Получение коннекторов фитинга
                ConnectorSet fittingConnectors = newFamilyInstance.MEPModel.ConnectorManager.Connectors;
                List<Connector> connectors = fittingConnectors.Cast<Connector>().ToList();
                if (connectors.Count < 2)
                {
                    TaskDialog.Show("Ошибка", "Фитинг должен иметь не менее двух коннекторов.");
                    return null;
                }

                // Сортировка коннекторов фитинга для определения, какой из них ближе к какой трубе
                Connector fittingConn1 = FindBestConnectorMatch(connectors, conn1);
                // Вторым коннектором будет любой другой коннектор, отличный от первого
                Connector fittingConn2 = connectors.FirstOrDefault(c => c.Id != fittingConn1.Id);

                if (fittingConn1 == null || fittingConn2 == null)
                {
                    TaskDialog.Show("Ошибка", "Не удалось определить подходящие коннекторы фитинга.");
                    return null;
                }

                // Сначала выравниваем фитинг относительно первого коннектора
                AlignConnectors(conn1, fittingConn1, newFamilyInstance);

                // Перемещаем фитинг к первому коннектору
                XYZ translationVector = conn1.Origin - fittingConn1.Origin;
                ElementTransformUtils.MoveElement(_doc, newFamilyInstance.Id, translationVector);

                // Соединяем первую пару коннекторов
                conn1.ConnectTo(fittingConn1);
                ElementTransformUtils.MoveElement(_doc, newFamilyInstance.Id, conn2.Origin - fittingConn2.Origin);
                // Соединяем вторую пару коннекторов
                conn2.ConnectTo(fittingConn2);

                return newFamilyInstance;
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка при создании муфты", ex.Message);
            return null;
        }

        return null;
    }

    private void AlignConnectors(Connector targetConnector, Connector attachingConnector, Element attachingElement)
    {
        // Получаем нормализованные векторы BasisZ коннекторов
        XYZ targetBasisZ = targetConnector.CoordinateSystem.BasisZ.Normalize();
        XYZ attachingBasisZ = attachingConnector.CoordinateSystem.BasisZ.Normalize();

        // Желаемое направление для attachingBasisZ - противоположное targetBasisZ
        XYZ desiredDirection = -targetBasisZ;

        // Вычисляем скалярное произведение между attachingBasisZ и желаемым направлением
        double dotProduct = attachingBasisZ.DotProduct(desiredDirection);

        // Корректируем значение dotProduct на случай погрешностей вычислений
        dotProduct = Math.Min(Math.Max(dotProduct, -1.0), 1.0);

        // Вычисляем угол между attachingBasisZ и desiredDirection
        double angle = Math.Acos(dotProduct);

        // Вычисляем ось вращения
        XYZ rotationAxis = attachingBasisZ.CrossProduct(desiredDirection);

        // Если ось вращения имеет нулевую длину (векторы параллельны или антипараллельны)
        if (rotationAxis.IsZeroLength())
        {
            // Векторы параллельны или антипараллельны
            if (dotProduct < -0.9999)
            {
                // Векторы направлены в ту же сторону, нужно вращение на 180 градусов
                angle = Math.PI;

                // Выбираем произвольную ось вращения, перпендикулярную attachingBasisZ
                rotationAxis = attachingConnector.CoordinateSystem.BasisX;

                if (rotationAxis.IsZeroLength())
                {
                    rotationAxis = attachingConnector.CoordinateSystem.BasisY;
                }
            }
            else
            {
                // Векторы уже направлены в противоположные стороны, вращение не требуется
                angle = 0;
            }
        }
        else
        {
            // Нормализуем ось вращения
            rotationAxis = rotationAxis.Normalize();
        }

        // Выполняем вращение, если угол больше допустимого порога
        if (angle > 1e-6)
        {
            // Создаем неограниченную линию вращения с началом в attachingConnector.Origin и направлением rotationAxis
            Line rotationLine = Line.CreateUnbound(attachingConnector.Origin, rotationAxis);

            // Вращаем присоединяемый элемент
            ElementTransformUtils.RotateElement(_doc, attachingElement.Id, rotationLine, angle);
        }
    }

    private Connector FindBestConnectorMatch(List<Connector> fittingConnectors, Connector pipeConnector)
    {
        // Сортировка коннекторов по сходству направления (противоположные направления лучше)
        return fittingConnectors
            .OrderBy(fc =>
            {
                double dotProduct = fc.CoordinateSystem.BasisZ.DotProduct(pipeConnector.CoordinateSystem.BasisZ);
                // Для противоположных направлений dotProduct ≈ -1, что нам и нужно
                return Math.Abs(dotProduct + 1.0);
            })
            .FirstOrDefault();
    }

    private static void SetDiameterFitting(PipeWrapper pipe, FamilyInstance familyInstance)
    {
        double? pipeDiameter = pipe.GetDiameter();
        Parameter fittingDiameterParam = null;
        Parameter param = familyInstance.FindParameter("Диаметр");
        if (param is { StorageType: StorageType.Double })
        {
            fittingDiameterParam = param;
        }

        if (fittingDiameterParam != null)
        {
            // Устанавливаем диаметр фитинга равный диаметру трубы
            if (pipeDiameter != null) fittingDiameterParam.Set((double)pipeDiameter);
        }
        else
        {
            // Если не нашли параметр диаметра, выведем предупреждение
            TaskDialog.Show("Предупреждение",
                "Не удалось найти параметр диаметра для муфты. Соединение может не работать.");
        }
    }

    /// <summary>
    /// Объединяет несколько DisplacementElement в один
    /// </summary>
    /// <summary>
    /// Объединяет несколько DisplacementElement в один
    /// </summary>
    private static void MergeDisplacementElements(Document doc, IList<DisplacementElement> displacementsToMerge,
        DisplacementElement primaryDisplacement)
    {
        if (displacementsToMerge == null || displacementsToMerge.Count <= 1)
            return; // Нечего объединять


        // Собираем все ID смещенных элементов из всех DisplacementElement
        HashSet<ElementId> allDisplacedElementIds = new HashSet<ElementId>();
        View targetView = null;

        XYZ displacementVector = primaryDisplacement.GetRelativeDisplacement();

        // Сохраняем список всех элементов для проверки
        Dictionary<ElementId, DisplacementElement> elementToDisplacement =
            new Dictionary<ElementId, DisplacementElement>();

        foreach (DisplacementElement disp in displacementsToMerge)
        {
            // Получаем смещенные элементы
            ICollection<ElementId> elementIds = disp.GetDisplacedElementIds();
            foreach (ElementId id in elementIds)
            {
                elementToDisplacement[id] = disp;
                allDisplacedElementIds.Add(id);
            }

            // Убедимся, что все DisplacementElement находятся на одном виде
            if (targetView == null)
            {
                targetView = doc.GetElement(disp.OwnerViewId) as View;
            }
        }

        // Важно: удаляем элементы из их текущих DisplacementElement перед созданием нового
        foreach (DisplacementElement disp in displacementsToMerge)
        {
            foreach (var element in disp.GetDisplacedElementIds())
            {
                disp.RemoveDisplacedElement(doc.GetElement(element));
            }
        }

        // Проверка, что элементы можно смещать
        List<ElementId> validElementIds = new List<ElementId>();
        foreach (ElementId id in allDisplacedElementIds)
        {
            if (DisplacementElement.IsAllowedAsDisplacedElement(doc.GetElement(id)))
            {
                validElementIds.Add(id);
            }
        }

        // Создаем новый DisplacementElement со всеми смещенными элементами
        if (validElementIds.Count > 0 && targetView != null)
        {
            // Создаем новый DisplacementElement
            DisplacementElement newDisplacement = DisplacementElement.Create(
                doc,
                validElementIds,
                displacementVector,
                targetView,
                null); // null означает, что это будет корневой DisplacementElement

            // Удаляем исходные (теперь пустые) DisplacementElement
            List<ElementId> displacementIds = new List<ElementId>();
            foreach (DisplacementElement disp in displacementsToMerge)
            {
                displacementIds.Add(disp.Id);
            }

            doc.Delete(displacementIds);

            // Удаляем пути смещения, если нужно
            FilteredElementCollector pathCollector = new FilteredElementCollector(doc, targetView.Id)
                .OfClass(typeof(DisplacementPath));

            List<ElementId> pathsToDelete = new List<ElementId>();
            foreach (Element path in pathCollector)
            {
                pathsToDelete.Add(path.Id);
            }

            if (pathsToDelete.Count > 0)
            {
                doc.Delete(pathsToDelete);
            }
        }
    }

    private static Connector NearestConnector(Connector[] connectors, XYZ startPoint)
    {
        if (connectors == null)
            return null;
        if (connectors.Length == 1)
            return connectors[0];
        Connector connector = null;
        double num1 = double.MaxValue;
        for (int index = 0; index < connectors.Count(); ++index)
        {
            double num2 = connectors[index].Origin.DistanceTo(startPoint);
            if (!(num2 < num1)) continue;
            num1 = num2;
            connector = connectors[index];
        }

        return connector;
    }


    // <summary>
    /// <summary>
    /// Находит тип семейства "Разрыв" среди доступных фитингов
    /// </summary>
    /// <returns>Тип семейства "Разрыв" или null, если не найден</returns>
    public FamilySymbol FindFamily(string familyName)
    {
        // Получаем все символы семейств из категории "Соединительные детали трубопроводов"
        FilteredElementCollector collector = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .OfClass(typeof(FamilySymbol));

        foreach (var element in collector)
        {
            var symbol = (FamilySymbol)element;
            // Проверяем имя семейства и имя типа
            if (symbol.Family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase) ||
                symbol.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase) ||
                symbol.Family.Name.Contains(familyName) ||
                symbol.Name.Contains(familyName))
            {
                return symbol;
            }
        }

        return null;
    }

    /// <summary>
    /// Определяет среднюю трубу между двумя точками по расстоянию
    /// </summary>
    private Pipe DetermineMidPipeByDistance(PipeWrapper pipe1, PipeWrapper pipe2, XYZ point1, XYZ point2)
    {
        // Вычисляем средние точки каждой трубы
        XYZ midPoint1 = pipe1.GetPipeCenter();
        XYZ midPoint2 = pipe2.GetPipeCenter();

        // Вычисляем центральную точку между точками разреза
        XYZ midPointBetweenCuts = (point1 + point2) * 0.5;

        // Вычисляем расстояния от средних точек труб до центральной точки между разрезами
        double distance1 = midPoint1.DistanceTo(midPointBetweenCuts);
        double distance2 = midPoint2.DistanceTo(midPointBetweenCuts);

        // Возвращаем трубу, средняя точка которой ближе к центральной точке между разрезами
        return distance1 < distance2 ? pipe1.Pipe : pipe2.Pipe;
    }

    public void BringBackVisibilityPipe(FamilySymbol familySymbol)
    {
        var selectReference =
            SelectReference("Выберите первую точку на трубе", new FamilySelectionFilter(familySymbol));
        var element = _doc.GetElement(selectReference);
        if (element is FamilyInstance familyInstance)
        {
            var activeView = familyInstance.Document.ActiveView;
            string param = String.Empty;
            if (activeView.ViewType == ViewType.ThreeD)
            {
                param = "msh_Разрыв";
            }

            ConnectorSet fittingConnectors = familyInstance.MEPModel.ConnectorManager.Connectors;
            List<Connector> connectors = fittingConnectors.Cast<Connector>().ToList();
            Transaction transaction = new Transaction(_doc, "Вернуть видимость трубы");
            try
            {
                transaction.Start();
                foreach (var connector in connectors)
                {
                    var enumerable = connector.AllRefs.Cast<Connector>().ToList();
                    foreach (var c in enumerable)
                    {
                        var paramValue = c.Owner.FindParameter(param);
                        if (paramValue != null && paramValue.AsBool())
                        {
                            paramValue.Set(false);
                        }
                    }
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.RollBack();
            }
        }
    }
}