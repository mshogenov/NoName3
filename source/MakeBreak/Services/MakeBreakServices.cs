using System.IO;
using System.Reflection;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MakeBreak.Filters;
using MakeBreak.Models;
using Nice3point.Revit.Toolkit.Options;
using NoNameApi.Extensions;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;


namespace MakeBreak.Services;

public class MakeBreakServices
{
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private readonly Document _doc = Context.ActiveDocument;
    private const string _parameterName_msh_Break_3D = "msh_Разрыв_3D";
    private const string _parameterName_msh_Break_Plan = "msh_Разрыв_План";

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

    private void CreateDisplacementElement(Break break1, PipeWrp secondSplitPipe, PipeWrp midPipe,
        PipeWrp firstSplitPipe, FamilyInstance secondCoupling, DisplacementElement displacementCreate1)
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

    private PipeWrp GetMidPipe(PipeWrp secondPipeToCut, Break break1, PipeWrp secondSplitPipe, Break break2,
        (PipeWrp SplitPipe, FamilyInstance Coupling)? firstBreak, PipeWrp firstSplitPipe)
    {
        PipeWrp midPipe = null;
        if (secondPipeToCut != null && break1.TargetPipe != null && secondPipeToCut.Id.Equals(break1.TargetPipe.Id) ||
            secondPipeToCut != null && firstBreak != null &&
            secondPipeToCut.Id.Equals(firstSplitPipe.Id))
        {
            midPipe = new PipeWrp(DetermineMidPipeByDistance(secondPipeToCut, secondSplitPipe, break1.BreakPoint,
                break2.BreakPoint));
        }

        return midPipe;
    }

    private static PipeWrp GetPipeToCut(Break break1, PipeWrp firstSplitPipe,
        Break break2)
    {
        PipeWrp pipeToCut;

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

    private DisplacementElement DisplacementCreate(Break break1, PipeWrp firstSplitPipe,
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

    private (PipeWrp SplitPipe, FamilyInstance Coupling)? InsertBreak(FamilySymbol familySymbol, Break break1)
    {
        ElementId firstSplitPipeId = PlumbingUtils.BreakCurve(_doc, break1.TargetPipe.Id, break1.BreakPoint);
        // Разрезаем трубу в первой точке

        if (firstSplitPipeId == ElementId.InvalidElementId)
        {
            return null;
        }

        PipeWrp firstSplitPipe =
            new PipeWrp(_doc.GetElement(firstSplitPipeId) as Pipe); // Вторая часть - новая труба
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
                Parameter commentParam = pipe?.FindParameter(_parameterName_msh_Break_3D);
                commentParam?.Set(true);
                break;
            }
            case ViewType.FloorPlan:
            {
                Parameter commentParam = pipe?.FindParameter(_parameterName_msh_Break_Plan);
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
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            TaskDialog.Show("Ошибка", "Произошла ошибка: " + e.Message);
        }

        return null;
    }

    private FamilyInstance CreateCouplingBetweenPipes(Break bBreak, PipeWrp splitPipe, FamilySymbol familySymbol)
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
                Connector fittingConn1 =
                    FindClosestConnector(newFamilyInstance.MEPModel.ConnectorManager, bBreak.BreakPoint);
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

    private Connector FindClosestConnector(ConnectorManager connectorManager, XYZ pickedPoint)
    {
        Connector closestConnector = null;

        // Все соединители элемента
        var connectors = connectorManager.Connectors.Cast<Connector>();

        double closestDistance = double.MaxValue;

        foreach (Connector connector in connectors)
        {
            if (connector.IsConnected)
            {
                continue;
            }

            // Координаты текущего соединителя
            XYZ connectorOrigin = connector.Origin;

            // Расстояние между выбранной точкой и соединителем
            double distance = pickedPoint.DistanceTo(connectorOrigin);

            // Проверяем, является ли это расстояние минимальным
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestConnector = connector;
            }
        }

        return closestConnector;
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

    private static void SetDiameterFitting(PipeWrp pipe, FamilyInstance familyInstance)
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
        Dictionary<ElementId, DisplacementElement> elementToDisplacement = [];

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
    private Pipe DetermineMidPipeByDistance(PipeWrp pipe1, PipeWrp pipe2, XYZ point1, XYZ point2)
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
        while (true)
        {
            var selectReference =
                SelectReference("Выберите первую точку на трубе", new BreakSelectionFilter(familySymbol));
            if (selectReference == null) return;
            FamilyInstance familyInstance = null;
            var selectedElement = _doc.GetElement(selectReference);
            switch (selectedElement)
            {
                case FamilyInstance family:
                    familyInstance = family;
                    break;
                case DisplacementElement displacement:
                {
                    familyInstance = FindElementInDisplacement(displacement, selectReference.GlobalPoint);

                    break;
                }
            }

            if (familyInstance == null) return;
            var activeView = familyInstance.Document.ActiveView;
            string param = string.Empty;
            switch (activeView.ViewType)
            {
                case ViewType.ThreeD:
                    param = _parameterName_msh_Break_3D;
                    break;
                case ViewType.FloorPlan:
                    param = _parameterName_msh_Break_Plan;
                    break;
            }


            ConnectorSet fittingConnectors = familyInstance.MEPModel.ConnectorManager.Connectors;
            List<Connector> connectors = fittingConnectors.Cast<Connector>().ToList();
            Transaction transaction = new Transaction(_doc, "Показать трубу");
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

    private FamilyInstance FindElementInDisplacement(DisplacementElement displacement, XYZ pickPoint)
    {
        var displacementElementIds = displacement.GetDisplacedElementIds();
        double toleranceInMm = 100.0;
        double tolerance = UnitUtils.ConvertToInternalUnits(toleranceInMm, UnitTypeId.Millimeters);

        foreach (ElementId displacedId in displacementElementIds)
        {
            Element element = _doc.GetElement(displacedId);
            if (element is not FamilyInstance instance) continue;
            if (instance.Name != "Разрыв") continue;
            if (IsFamilyInstanceAtPoint(instance, pickPoint, tolerance))
            {
                return instance;
            }
        }

        return null;
    }

    private bool IsFamilyInstanceAtPoint(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        // Способ 1: Расширенный BoundingBox
        if (CheckBoundingBoxContains(instance, pickPoint, tolerance))
            return true;

        // Способ 2: Проверка через геометрию
        if (CheckGeometryContains(instance, pickPoint, tolerance))
            return true;

        // Способ 3: Проверка через Location (для точечных элементов)
        if (CheckLocationDistance(instance, pickPoint, tolerance))
            return true;

        return false;
    }

    private bool CheckBoundingBoxContains(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        try
        {
            BoundingBoxXYZ bounding = instance.get_BoundingBox(_doc.ActiveView);

            // Если bounding для активного вида null, попробуем без вида
            if (bounding == null)
            {
                bounding = instance.get_BoundingBox(null);
            }

            if (bounding == null) return false;

            // Расширяем BoundingBox
            XYZ expandVector = new XYZ(tolerance, tolerance, tolerance);

            XYZ min = bounding.Min.Subtract(expandVector);
            XYZ max = bounding.Max.Add(expandVector);

            return pickPoint.X >= min.X && pickPoint.X <= max.X &&
                   pickPoint.Y >= min.Y && pickPoint.Y <= max.Y &&
                   pickPoint.Z >= min.Z && pickPoint.Z <= max.Z;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckBoundingBoxContains: {ex.Message}");
            return false;
        }
    }

    private bool CheckGeometryContains(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        try
        {
            Options geometryOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Medium,
                IncludeNonVisibleObjects = false,
                ComputeReferences = false
            };

            GeometryElement geometryElement = instance.get_Geometry(geometryOptions);
            if (geometryElement == null) return false;

            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (IsPointNearFamilyGeometry(geometryObject, pickPoint, tolerance))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckGeometryContains: {ex.Message}");
        }

        return false;
    }

    private bool CheckLocationDistance(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        try
        {
            if (instance.Location is LocationPoint locationPoint)
            {
                double distance = locationPoint.Point.DistanceTo(pickPoint);
                return distance <= tolerance;
            }
            else if (instance.Location is LocationCurve locationCurve)
            {
                Curve curve = locationCurve.Curve;
                double distance = curve.Distance(pickPoint);
                return distance <= tolerance;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckLocationDistance: {ex.Message}");
        }

        return false;
    }

    private bool IsPointNearFamilyGeometry(GeometryObject geometryObject, XYZ point, double tolerance)
    {
        try
        {
            switch (geometryObject)
            {
                case Solid solid when solid.Volume > 0:
                    return IsPointNearSolid(solid, point, tolerance);

                case GeometryInstance instance:
                    Transform transform = instance.Transform;
                    foreach (GeometryObject obj in instance.GetInstanceGeometry())
                    {
                        // Преобразуем точку в локальную систему координат
                        XYZ localPoint = transform.Inverse.OfPoint(point);
                        if (IsPointNearFamilyGeometry(obj, localPoint, tolerance))
                            return true;
                    }

                    break;

                case Curve curve:
                    return curve.Distance(point) <= tolerance;

                case Face face:
                    try
                    {
                        IntersectionResult result = face.Project(point);
                        if (result != null)
                        {
                            return result.Distance <= tolerance;
                        }
                    }
                    catch
                    {
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in IsPointNearFamilyGeometry: {ex.Message}");
        }

        return false;
    }

    private bool IsPointNearSolid(Solid solid, XYZ point, double tolerance)
    {
        try
        {
            // Сначала проверяем расширенный BoundingBox
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            if (bbox != null)
            {
                XYZ min = bbox.Min - new XYZ(tolerance, tolerance, tolerance);
                XYZ max = bbox.Max + new XYZ(tolerance, tolerance, tolerance);

                bool inExpandedBox = point.X >= min.X && point.X <= max.X &&
                                     point.Y >= min.Y && point.Y <= max.Y &&
                                     point.Z >= min.Z && point.Z <= max.Z;

                if (!inExpandedBox) return false;
            }

            // Проверяем каждую грань solid'а
            foreach (Face face in solid.Faces)
            {
                try
                {
                    IntersectionResult result = face.Project(point);
                    if (result != null && result.Distance <= tolerance)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in IsPointNearSolid: {ex.Message}");
        }

        return false;
    }

    public void DeleteBreaks(FamilySymbol familySymbol)
    {
        while (true)
        {
            var selectedBreak = SelectGap(familySymbol);
            if (selectedBreak?.FamilyInstance == null) return;
            Pipe generalPipe = null;
            Element deletePipe = null;
            Connector attachConnector = null;
            Connector targetConnector = null;
            XYZ targetPosition = null;
            var pairBreak = selectedBreak.FindPairBreak(familySymbol);
            if (pairBreak != null)
            {
                generalPipe = selectedBreak.FindGeneralPipe(pairBreak);
                if (generalPipe == null) return;

                var connectElementSelectedBreak =
                    selectedBreak.ConnectedElements.FirstOrDefault(x => x.Id != generalPipe.Id);

                if (connectElementSelectedBreak != null)
                {
                    foreach (var connectedElement in pairBreak.ConnectedElements.Where(connectedElement =>
                                 connectedElement.Id != generalPipe.Id))
                    {
                        deletePipe = connectedElement;
                    }

                    attachConnector = connectElementSelectedBreak.FindCommonConnector(selectedBreak.FamilyInstance);
                    if (deletePipe != null)
                    {
                        Element connectElementDeletePipe =
                            deletePipe.GetConnectedMEPElements().FirstOrDefault(x =>
                                x.Id != pairBreak.Id &&
                                x.Category.BuiltInCategory != BuiltInCategory.OST_PipeInsulations);

                        if (connectElementDeletePipe != null)
                        {
                            targetConnector = connectElementDeletePipe.FindCommonConnector(deletePipe);
                        }
                        else
                        {
                            targetPosition = deletePipe.GetConnectors().FirstOrDefault(x => !x.IsConnected)?.Origin;
                        }
                    }
                    else
                    {
                        targetPosition = pairBreak.Connectors.FirstOrDefault(x => !x.IsConnected)?.Origin;
                    }
                }
                else
                {
                    attachConnector = pairBreak.ConnectedElements
                        .FirstOrDefault(connectedElement => connectedElement.Id != generalPipe.Id)
                        .FindCommonConnector(pairBreak.FamilyInstance);
                    targetPosition = selectedBreak.Connectors.FirstOrDefault(x => !x.IsConnected)?.Origin;
                }
            }
            else
            {
                if (selectedBreak.ConnectedElements.Count > 1)
                {
                    if (selectedBreak.ConnectedElements.All(x => x is Pipe))
                    {
                        deletePipe = selectedBreak.ConnectedElements
                            .OrderBy(x => x.FindParameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble())
                            .FirstOrDefault();
                        if (deletePipe != null)
                        {
                            attachConnector = selectedBreak.ConnectedElements
                                .FirstOrDefault(connectedElement => connectedElement.Id != deletePipe.Id)
                                .FindCommonConnector(selectedBreak.FamilyInstance);

                            Element connectElementDeletePipe =
                                deletePipe.GetConnectedMEPElements().FirstOrDefault(x => x.Id != selectedBreak.Id);

                            if (connectElementDeletePipe != null)
                            {
                                targetConnector = connectElementDeletePipe.FindCommonConnector(deletePipe);
                            }
                            else
                            {
                                targetPosition = deletePipe.GetConnectors().FirstOrDefault(x => !x.IsConnected)?.Origin;
                            }
                        }
                    }
                    else
                    {
                        var connectElementSelectedBreak =
                            selectedBreak.ConnectedElements.FirstOrDefault(x => x is Pipe);
                        if (connectElementSelectedBreak != null)
                        {
                            attachConnector =
                                connectElementSelectedBreak.FindCommonConnector(selectedBreak.FamilyInstance);
                            targetConnector = selectedBreak.ConnectedElements
                                .FirstOrDefault(x => x.Id != connectElementSelectedBreak.Id)
                                .FindCommonConnector(selectedBreak.FamilyInstance);
                        }
                    }
                }
            }

            using Transaction trans = new Transaction(_doc, "Удалить разрыв");
            trans.Start();
            try
            {
                // Удаляем разрывы и промежуточную трубу
                _doc.Delete(selectedBreak.Id);
                if (pairBreak != null)
                {
                    _doc.Delete(pairBreak.Id);
                }

                if (generalPipe != null)
                {
                    _doc.Delete(generalPipe.Id);
                }

                if (deletePipe != null)
                {
                    _doc.Delete(deletePipe.Id);
                }

                if (targetConnector != null)
                {
                    LengthenCurve(attachConnector, targetConnector);
                    attachConnector.ConnectTo(targetConnector);
                }
                else
                {
                    LengthenCurveToPosition(attachConnector, targetPosition);
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.RollBack();
                TaskDialog.Show("Ошибка", ex.Message);
            }
        }
    }

    /// <summary>
    /// Если присоединяемый элемент является кривой, то ее длина удлиняется по направлению к соединяемому элементу
    /// </summary>
    /// <param name="attachingConnector"></param>
    /// <param name="targetConnector"></param>
    private static void LengthenCurve(Connector attachingConnector, Connector targetConnector)
    {
        if (attachingConnector == null || targetConnector == null) return;

        if (attachingConnector.Owner.Location is not LocationCurve locationCurve) return;
        // 1. Удлиняем трубу
        XYZ startPoint = locationCurve.Curve.GetEndPoint(0);
        XYZ endPoint = locationCurve.Curve.GetEndPoint(1);

        double startDistance = startPoint.DistanceTo(attachingConnector.Origin);
        double endDistance = endPoint.DistanceTo(attachingConnector.Origin);

        // Определяем, какой конец трубы ближе к соединителю элемента
        XYZ pointToExtend;
        XYZ otherPoint;
        XYZ pipeDirection;
        XYZ extensionPoint;
        Line newCurve;

        if (startDistance < endDistance)
        {
            // Удлиняем от startPoint
            pointToExtend = startPoint;
            otherPoint = endPoint;
            pipeDirection = (startPoint - endPoint).Normalize();

            // Вектор от точки удлинения до коннектора целевого элемента
            XYZ vectorToTarget = targetConnector.Origin - pointToExtend;

            // Расстояние вдоль направления pipeDirection
            double extensionLength = vectorToTarget.DotProduct(pipeDirection);

            // Вычисляем новую точку начала трубы
            extensionPoint = pointToExtend + pipeDirection * extensionLength;

            // Создаем новую линию (трубу) от extensionPoint до endPoint (otherPoint)
            newCurve = Line.CreateBound(extensionPoint, otherPoint);
        }
        else
        {
            // Удлиняем от endPoint
            pointToExtend = endPoint;
            otherPoint = startPoint;
            pipeDirection = (endPoint - startPoint).Normalize();

            // Вектор от точки удлинения до коннектора целевого элемента
            XYZ vectorToTarget = targetConnector.Origin - pointToExtend;

            // Расстояние вдоль направления pipeDirection
            double extensionLength = vectorToTarget.DotProduct(pipeDirection);
            if (extensionLength <= 0)
            {
                return;
            }

            // Вычисляем новую конечную точку трубы
            extensionPoint = pointToExtend + pipeDirection * extensionLength;

            // Создаем новую линию (трубу) от startPoint (otherPoint) до extensionPoint
            newCurve = Line.CreateBound(otherPoint, extensionPoint);
        }

        locationCurve.Curve = newCurve;
    }

    /// <summary>
    /// Удлиняет кривую до заданной позиции по направлению к этой позиции
    /// </summary>
    /// <param name="attachingConnector">Коннектор элемента, который нужно удлинить</param>
    /// <param name="targetPosition">Целевая позиция, до которой нужно удлинить</param>
    private static void LengthenCurveToPosition(Connector attachingConnector, XYZ targetPosition)
    {
        if (attachingConnector == null || targetPosition == null) return;


        if (attachingConnector.Owner.Location is not LocationCurve locationCurve) return;

        // Получаем точки кривой
        XYZ startPoint = locationCurve.Curve.GetEndPoint(0);
        XYZ endPoint = locationCurve.Curve.GetEndPoint(1);

        double startDistance = startPoint.DistanceTo(attachingConnector.Origin);
        double endDistance = endPoint.DistanceTo(attachingConnector.Origin);

        // Определяем, какой конец трубы ближе к соединителю элемента
        XYZ pointToExtend;
        XYZ otherPoint;
        XYZ pipeDirection;
        XYZ extensionPoint;
        Line newCurve;

        if (startDistance < endDistance)
        {
            // Удлиняем от startPoint
            pointToExtend = startPoint;
            otherPoint = endPoint;
            pipeDirection = (startPoint - endPoint).Normalize();

            // Вектор от точки удлинения до целевой позиции
            XYZ vectorToTarget = targetPosition - pointToExtend;

            // Расстояние вдоль направления pipeDirection
            double extensionLength = vectorToTarget.DotProduct(pipeDirection);

            if (extensionLength <= 0)
            {
                return; // Не удлиняем в обратном направлении
            }

            // Вычисляем новую точку начала трубы
            extensionPoint = pointToExtend + pipeDirection * extensionLength;

            // Создаем новую линию от extensionPoint до endPoint
            newCurve = Line.CreateBound(extensionPoint, otherPoint);
        }
        else
        {
            // Удлиняем от endPoint
            pointToExtend = endPoint;
            otherPoint = startPoint;
            pipeDirection = (endPoint - startPoint).Normalize();

            // Вектор от точки удлинения до целевой позиции
            XYZ vectorToTarget = targetPosition - pointToExtend;

            // Расстояние вдоль направления pipeDirection
            double extensionLength = vectorToTarget.DotProduct(pipeDirection);

            if (extensionLength <= 0)
            {
                return; // Не удлиняем в обратном направлении
            }

            // Вычисляем новую конечную точку трубы
            extensionPoint = pointToExtend + pipeDirection * extensionLength;

            // Создаем новую линию от startPoint до extensionPoint
            newCurve = Line.CreateBound(otherPoint, extensionPoint);
        }

        locationCurve.Curve = newCurve;
    }

// Вспомогательные методы
    private Element GetConnectedElementExcluding(Gap gap, ElementId excludeId)
    {
        return gap.ConnectedElements?.FirstOrDefault(e => e.Id != excludeId);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="element1"></param>
    /// <param name="element2"></param>
    /// <returns></returns>
    private Connector FindConnectorBetween(Element element1, Element element2)
    {
        var connectorManager = GetConnectorManager(element1);
        if (connectorManager?.Connectors == null) return null;

        foreach (Connector connector in connectorManager.Connectors)
        {
            if (!connector.IsConnected) continue;

            foreach (Connector connectedConnector in connector.AllRefs.Cast<Connector>())
            {
                if (connectedConnector.Owner.Id == element2.Id)
                    return connector;
            }
        }

        return null;
    }

    private void DisconnectElement(Element element)
    {
        var connectorManager = GetConnectorManager(element);
        if (connectorManager?.Connectors == null) return;

        foreach (Connector connector in connectorManager.Connectors)
        {
            if (connector.IsConnected)
            {
                connector.DisconnectFrom(connector.AllRefs.Cast<Connector>().FirstOrDefault());
            }
        }
    }

    private Connector GetFreeConnectorOnElement(Element element)
    {
        var connectorManager = GetConnectorManager(element);
        if (connectorManager?.Connectors == null) return null;

        foreach (Connector connector in connectorManager.Connectors)
        {
            if (!connector.IsConnected)
                return connector;
        }

        return null;
    }

    private void AlignConnectors(Element movableElement, Connector movableConnector, Connector targetConnector)
    {
        // Перемещаем элемент так, чтобы коннекторы совпали по позиции
        XYZ moveVector = targetConnector.Origin - movableConnector.Origin;
        ElementTransformUtils.MoveElement(_doc, movableElement.Id, moveVector);

        // Поворачиваем если нужно (для правильной ориентации)
        XYZ movableDirection = movableConnector.CoordinateSystem.BasisZ;
        XYZ targetDirection = targetConnector.CoordinateSystem.BasisZ;

        double angle = movableDirection.AngleTo(targetDirection);
        if (Math.Abs(angle - Math.PI) > 0.01) // Если угол не равен 180°
        {
            XYZ rotationAxis = movableDirection.CrossProduct(targetDirection);
            if (!rotationAxis.IsZeroLength())
            {
                Line rotationLine = Line.CreateBound(
                    targetConnector.Origin,
                    targetConnector.Origin + rotationAxis);
                ElementTransformUtils.RotateElement(_doc, movableElement.Id, rotationLine, Math.PI - angle);
            }
        }
    }

    private ConnectorManager GetConnectorManager(Element element)
    {
        if (element is MEPCurve mepCurve)
            return mepCurve.ConnectorManager;
        else if (element is FamilyInstance familyInstance)
            return familyInstance.MEPModel?.ConnectorManager;

        return null;
    }

// Вспомогательные методы для работы с Gap
    private Connector FindConnectorToElement(Gap gap, Element targetElement)
    {
        foreach (var connector in gap.Connectors)
        {
            if (connector.IsConnected)
            {
                foreach (Connector connectedConnector in connector.AllRefs.Cast<Connector>())
                {
                    if (connectedConnector.Owner.Id == targetElement.Id)
                    {
                        return connector;
                    }
                }
            }
        }

        return null;
    }

    private Connector FindFreeConnectorInElement(Element element)
    {
        // Для MEP элементов
        if (element is MEPCurve mepCurve)
        {
            var connectorSet = mepCurve.ConnectorManager?.Connectors;
            if (connectorSet != null)
            {
                foreach (Connector connector in connectorSet)
                {
                    if (!connector.IsConnected)
                        return connector;
                }
            }
        }
        // Для FamilyInstance (фитинги, оборудование)
        else if (element is FamilyInstance familyInstance)
        {
            var connectorSet = familyInstance.MEPModel?.ConnectorManager?.Connectors;
            if (connectorSet != null)
            {
                foreach (Connector connector in connectorSet)
                {
                    if (!connector.IsConnected)
                        return connector;
                }
            }
        }

        return null;
    }

    private Gap SelectGap(FamilySymbol familySymbol)
    {
        try
        {
            var reference = _uidoc.Selection
                .PickObject(ObjectType.Element, new BreakSelectionFilter(familySymbol));
            if (reference != null)
            {
                return new Gap(reference, _doc);
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        return null;
    }


    private void CreatePipeBetweenElements(Element start, Element end)
    {
        try
        {
            // Проверяем существование элементов
            if (start == null || end == null ||
                _doc.GetElement(start.Id) == null ||
                _doc.GetElement(end.Id) == null)
                return;

            // Получаем коннекторы
            Connector startConn = GetAvailableConnector(start);
            Connector endConn = GetAvailableConnector(end);

            if (startConn != null && endConn != null)
            {
                // Получаем параметры от существующей трубы
                MEPCurve existingPipe = start as MEPCurve;
                if (existingPipe == null)
                {
                    var connectedPipes = GetConnectedPipes(start);
                    existingPipe = connectedPipes.FirstOrDefault();
                }

                if (existingPipe != null)
                {
                    ElementId systemTypeId = existingPipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)
                        ?.AsElementId();
                    ElementId pipeTypeId =
                        existingPipe.get_Parameter(BuiltInParameter.RBS_PIPE_TYPE_PARAM)?.AsElementId();
                    ElementId levelId = existingPipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM)
                        ?.AsElementId();

                    if (systemTypeId != null && pipeTypeId != null && levelId != null)
                    {
                        Pipe.Create(_doc, systemTypeId, pipeTypeId, levelId,
                            startConn.Origin, endConn.Origin);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", "Failed to create pipe: " + ex.Message);
        }
    }

    private List<MEPCurve> GetConnectedPipes(Element element)
    {
        var connectedPipes = new List<MEPCurve>();
        try
        {
            if (element is FamilyInstance familyInstance &&
                familyInstance.MEPModel?.ConnectorManager != null)
            {
                var connectors = familyInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>();
                foreach (var connector in connectors)
                {
                    if (connector.IsConnected)
                    {
                        foreach (Connector ref_connector in connector.AllRefs)
                        {
                            if (ref_connector.Owner is MEPCurve mepCurve)
                            {
                                connectedPipes.Add(mepCurve);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Игнорируем ошибки
        }

        return connectedPipes;
    }

    private Connector GetAvailableConnector(Element element)
    {
        try
        {
            if (element == null || element.Id == null || _doc.GetElement(element.Id) == null)
                return null;

            if (element is MEPCurve mepCurve && mepCurve.ConnectorManager != null)
            {
                return mepCurve.ConnectorManager.Connectors.Cast<Connector>()
                    .FirstOrDefault(c => c.IsConnected); // Изменено на IsConnected
            }
            else if (element is FamilyInstance familyInstance &&
                     familyInstance.MEPModel != null &&
                     familyInstance.MEPModel.ConnectorManager != null)
            {
                return familyInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>()
                    .FirstOrDefault(c => c.IsConnected); // Изменено на IsConnected
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    public ParameterFilterElement AddFilter(List<BuiltInCategory> mepCategories,
        string filterName, string parameterName, bool expected)
    {
        var categoryIds = mepCategories
            .Where(c => c != 0)
            .Select(c => new ElementId(c))
            .ToList();

        if (categoryIds.Count == 0)
        {
            TaskDialog.Show("Ошибка", "Список категорий МЕП пуст.");
            return null;
        }

        // Проверяем существование фильтра ПЕРЕД созданием
        var existingFilter = new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>()
            .FirstOrDefault(f => f.Name.Equals(filterName, StringComparison.OrdinalIgnoreCase));

        if (existingFilter != null)
        {
            TaskDialog.Show("Ошибка", $"Фильтр «{filterName}» уже существует.");
            return existingFilter;
        }

        // Создаём фильтр
        ParameterFilterElement parameterFilter =
            ParameterFilterElement.Create(_doc, filterName, categoryIds);

        try
        {
            ParameterElement paramElement = GetParameterElement(parameterName);
            if (paramElement == null)
            {
                TaskDialog.Show("Ошибка", $"Параметр «{parameterName}» не найден.");
                _doc.Delete(parameterFilter.Id); // удаляем созданный фильтр
                return null;
            }

            int boolInt = expected ? 1 : 0;
            FilterRule rule = ParameterFilterRuleFactory
                .CreateEqualsRule(paramElement.Id, boolInt);

            ElementParameterFilter elementFilter = new ElementParameterFilter(rule);
            parameterFilter.SetElementFilter(elementFilter);

            TaskDialog.Show("Успех", $"Фильтр «{filterName}» создан успешно.");
            return parameterFilter;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", $"Не удалось настроить правило фильтра: {ex.Message}");
            _doc.Delete(parameterFilter.Id); // удаляем созданный фильтр при ошибке
            return null;
        }
    }

    private ParameterElement GetParameterElement(string parameterName)
    {
        return new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterElement))
            .Cast<ParameterElement>()
            .FirstOrDefault(pe => pe.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
    }

    public void ApplyFilterToView(View activeView, ParameterFilterElement filter, bool filterVisibility)
    {
        if (activeView.GetFilters().Contains(filter.Id)) return;
        try
        {
            activeView.AddFilter(filter.Id);
            activeView.SetFilterVisibility(filter.Id, filterVisibility);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка",
                $"Не удалось применить фильтр '{filter.Name}' к виду '{activeView.Name}'.\n{ex.Message}");
        }
    }

    public void DownloadFamily(string familyName)
    {
        // Получаем текущую сборку
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"MakeBreak.Resources.{familyName}.rfa";
        // Проверяем, существует ли ресурс
        string[] resourceNames = assembly.GetManifestResourceNames();
        if (!resourceNames.Contains(resourceName))
        {
            return;
        }

        Stream stream = assembly.GetManifestResourceStream(resourceName);
        // Создаем временный файл с желаемым именем семейства
        string tempFamilyFileName = $"{familyName}.rfa";
        string tempFamilyPath = Path.Combine(Path.GetTempPath(), tempFamilyFileName);

        // Проверяем, существует ли файл, и удаляем его, если необходимо
        if (File.Exists(tempFamilyPath))
        {
            File.Delete(tempFamilyPath);
        }

        //Сохраняем поток в файл
        using (FileStream fileStream = new FileStream(tempFamilyPath, FileMode.Create, FileAccess.Write))
        {
            stream?.CopyTo(fileStream);
        }

        bool loaded = _doc.LoadFamily(tempFamilyPath, new FamilyLoadOptions(), out Family family);

        if (loaded && family != null)
        {
            TaskDialog.Show("Успешно", "Семейство было успешно загружено.");
        }
        else
        {
            TaskDialog.Show("Ошибка", "Не удалось загрузить семейство.");
        }

        File.Delete(tempFamilyPath);
    }

    public void HidePipe(FamilySymbol familySymbol)
    {
        while (true)
        {
            var selectedBreak = SelectGap(familySymbol);
            if (selectedBreak?.FamilyInstance == null) return;
            var pairBreak = selectedBreak.FindPairBreak(familySymbol);
            if (pairBreak == null) return;
            var generalPipe = selectedBreak.FindGeneralPipe(pairBreak);
            if (generalPipe == null) return;
            string param = string.Empty;
            var activeView = generalPipe.Document.ActiveView;
            switch (activeView.ViewType)
            {
                case ViewType.ThreeD:
                    param = _parameterName_msh_Break_3D;
                    break;
                case ViewType.FloorPlan:
                    param = _parameterName_msh_Break_Plan;
                    break;
            }


            Transaction transaction = new Transaction(_doc, "Скрыть трубу");
            try
            {
                transaction.Start();

                var paramValue = generalPipe.FindParameter(param);
                if (paramValue != null && !paramValue.AsBool())
                {
                    paramValue.Set(true);
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