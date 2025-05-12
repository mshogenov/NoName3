using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MakeBreak.Filters;


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
            if (selectReference1 == null) return;
            XYZ point1 = selectReference1.GlobalPoint;
            // Сохраняем оригинальные точки для дальнейших расчетов
            XYZ originalPoint1 = new XYZ(point1.X, point1.Y, point1.Z);
            // Получаем трубу по ID
            ElementId selectReferenceId = selectReference1.ElementId;
            Pipe originalPipe = GetOriginalPipe(selectReferenceId, point1);

            // Проверяем, нашли ли мы трубу
            if (originalPipe == null)
            {
                TaskDialog.Show("Ошибка", "Не удалось найти трубу в указанной точке");
                return;
            }

            using TransactionGroup tg = new TransactionGroup(_doc, "Сделать разрыв");
            try
            {
                tg.Start();
                using Transaction transaction = new Transaction(_doc, "Вставка первой муфты");
                transaction.Start();
                // Разрезаем трубу в первой точке
                ElementId firstSplitPipeId = PlumbingUtils.BreakCurve(_doc, originalPipe.Id, point1);
                if (firstSplitPipeId == ElementId.InvalidElementId)
                {
                    TaskDialog.Show("Ошибка", "Не удалось разрезать трубу в первой точке.");
                    transaction.RollBack();
                    tg.RollBack();
                    return;
                }

                Pipe secondPipe = _doc.GetElement(firstSplitPipeId) as Pipe; // Вторая часть - новая труба
                // Создаем муфту между первой и второй частью (используя "Разрыв")
                FamilyInstance firstCoupling = CreateCouplingBetweenPipes(originalPipe, secondPipe, familySymbol);
                if (firstCoupling == null)
                {
                    TaskDialog.Show("Предупреждение", "Не удалось создать муфту в первой точке.");
                    transaction.RollBack();
                    tg.RollBack();
                    return;
                }

                transaction.Commit();
                using Transaction trans2 = new Transaction(_doc, "Вставка второй муфты");
                trans2.Start();
                Reference refPipe2 = SelectReference("Выберите вторую точку на трубе", new SelectionFilter());
                if (refPipe2 == null)
                {
                    tg.RollBack();
                    return;
                }

                XYZ point2 = refPipe2.GlobalPoint;
                // Проверяем минимальное расстояние между точками
                double distanceBetweenPoints = point1.DistanceTo(point2).ToMillimeters();
                const double minimumDistance = 20; //мм

                if (distanceBetweenPoints < minimumDistance)
                {
                    TaskDialog.Show("Предупреждение",
                        $"Выбранные точки расположены слишком близко друг к другу (расстояние: {distanceBetweenPoints} миллиметров). " +
                        $"Минимальное допустимое расстояние: {minimumDistance} миллиметров. " + "Операция отменена.");
                    trans2.RollBack();
                    tg.RollBack();
                    return;
                }

                XYZ originalPoint2 = new XYZ(point2.X, point2.Y, point2.Z);
                // Определяем, какую трубу разрезать для второй точки

                ElementId secondCutPipeId;

                // Проверяем, какая из труб после разрезания имеет такой же ElementId или OST_ID как выбранная точка
                if (originalPipe?.Id.Value == refPipe2.ElementId.Value)
                {
                    secondCutPipeId = originalPipe.Id;
                }
                else if (secondPipe?.Id.Value == refPipe2.ElementId.Value)
                {
                    secondCutPipeId = secondPipe.Id;
                }
                else
                {
                    // Используем запасной вариант - проверка по расстоянию
                    double dist1 = DistanceFromPipeToPont(originalPipe, originalPoint2);
                    double dist2 = DistanceFromPipeToPont(secondPipe, originalPoint2);
                    secondCutPipeId = dist1 < dist2 ? originalPipe?.Id : secondPipe?.Id;
                }

                Pipe midPipe = null;
                // Получаем центральную линию трубы
                if (_doc.GetElement(secondCutPipeId) is Pipe pipeToCut)
                {
                    LocationCurve locationCurve = pipeToCut.Location as LocationCurve;
                    Curve pipeCurve = locationCurve?.Curve;
                    // Проецируем выбранную точку на центральную линию трубы
                    IntersectionResult result = pipeCurve?.Project(originalPoint2);
                    if (result == null)
                    {
                        TaskDialog.Show("Ошибка", "Не удалось спроецировать точку на трубу.");
                        trans2.RollBack();
                        tg.RollBack();
                        return;
                    }

                    XYZ projectedPoint = result.XYZPoint;
                    // Теперь используем спроецированную точку для разрезания
                    ElementId thirdPipeId = PlumbingUtils.BreakCurve(_doc, secondCutPipeId, projectedPoint);
                    Pipe thirdPipe = _doc.GetElement(thirdPipeId) as Pipe;
                    // Создаем муфту между разрезанными частями (используя "Разрыв")
                    FamilyInstance secondCoupling = CreateCouplingBetweenPipes(pipeToCut, thirdPipe, familySymbol);
                    if (secondCoupling == null)
                    {
                        TaskDialog.Show("Предупреждение", "Не удалось создать муфту во второй точке.");
                        trans2.RollBack();
                        tg.RollBack();
                        return;
                    }

                    // Определяем среднюю трубу между двумя точками разреза

                    if (secondCutPipeId != null && originalPipe != null && secondCutPipeId.Equals(originalPipe.Id))
                    {
                        midPipe = DetermineMidPipeByDistance(pipeToCut, thirdPipe, originalPoint1, originalPoint2);
                    }
                    else if (secondCutPipeId != null && secondPipe != null && secondCutPipeId.Equals(secondPipe.Id))
                    {
                        midPipe = DetermineMidPipeByDistance(pipeToCut, thirdPipe, originalPoint1, originalPoint2);
                    }

                    SetParameterBreak(midPipe);
                }

                trans2.Commit();
                // if (selectedElement is DisplacementElement displacement)
                // {
                //     CreateEnhancedDisplacement(_doc, new List<ElementId>()
                //     {
                //         originalPipe.Id,
                //         secondCutPipeId,
                //     }, new XYZ(0, 0, 50), null, null);
                // }

                tg.Assimilate();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
        }
    }

    private Pipe GetOriginalPipe(ElementId selectReferenceId, XYZ point1)
    {
        Pipe originalPipe = null;
        Element selectedElement = _doc.GetElement(selectReferenceId);
        switch (selectedElement)
        {
            case Pipe pipe:
                originalPipe = pipe;
                break;
            case DisplacementElement displacementElement:
            {
                var displacementElementIds = displacementElement.GetDisplacedElementIds();

                foreach (ElementId displacedId in displacementElementIds)
                {
                    Element element = _doc.GetElement(displacedId);

                    // Проверяем, является ли элемент трубой
                    if (element is not Pipe pipe) continue;
                    // Получаем геометрию трубы
                    BoundingBoxXYZ bounding = pipe.get_BoundingBox(_doc.ActiveView);
                    var contains = bounding.Contains(point1);
                    if (!contains) continue;
                    // Нашли трубу, которая проходит через точку
                    originalPipe = pipe;
                    break;
                }

                break;
            }
        }

        return originalPipe;
    }

    /// <summary>
    /// Создает новый DisplacementElement с добавлением новых элементов, используя расширенные параметры
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="elementsToDisplace">ID новых элементов для смещения</param>
    /// <param name="displacement">Вектор смещения</param>
    /// <param name="ownerView">3D вид, который будет владельцем DisplacementElement (может быть null для использования активного вида)</param>
    /// <param name="parentDisplacementElement">Родительский DisplacementElement (может быть null)</param>
    /// <returns>Новый созданный DisplacementElement</returns>
    public static DisplacementElement CreateEnhancedDisplacement(
        Document doc,
        ICollection<ElementId> elementsToDisplace,
        XYZ displacement,
        View ownerView = null,
        DisplacementElement parentDisplacementElement = null)
    {
        DisplacementElement newDisplacement = null;
        using Transaction transaction = new Transaction(doc, "Create New Displacement Element");
        try
        {
            transaction.Start();
            // Если вид не указан, попробуем найти активный 3D вид
            if (ownerView == null)
            {
                UIDocument uidoc = new UIDocument(doc);
                View activeView = doc.ActiveView;

                // Проверяем, является ли активный вид 3D видом
                if (activeView.ViewType == ViewType.ThreeD)
                {
                    ownerView = activeView;
                }
                else
                {
                    // Ищем первый доступный 3D вид
                    FilteredElementCollector viewCollector = new FilteredElementCollector(doc);
                    viewCollector.OfClass(typeof(View3D));

                    foreach (View3D view3D in viewCollector)
                    {
                        if (!view3D.IsTemplate)
                        {
                            ownerView = view3D;
                            break;
                        }
                    }

                    // Если 3D вид не найден, создаем новый
                    if (ownerView == null)
                    {
                        ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);

                        if (viewFamilyType != null)
                        {
                            View3D view3D = View3D.CreateIsometric(doc, viewFamilyType.Id);
                            ownerView = view3D;
                        }
                        else
                        {
                            throw new Exception("Не удалось найти или создать 3D вид");
                        }
                    }
                }
            }

            // Проверяем, что у нас есть вектор смещения
            if (displacement == null)
            {
                displacement = new XYZ(0, 0, 0);
            }

            // Создаем новый DisplacementElement с расширенными параметрами
            if (elementsToDisplace.Count > 0)
            {
                newDisplacement = DisplacementElement.Create(
                    doc,
                    elementsToDisplace,
                    displacement,
                    ownerView,
                    parentDisplacementElement);
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", "Ошибка при создании DisplacementElement: " + ex.Message);
        }


        return newDisplacement;
    }

    /// <summary>
    /// Создает новый DisplacementElement на основе существующего с добавлением новых элементов
    /// </summary>
    /// <summary>
    /// Создает новый DisplacementElement на основе существующего с добавлением новых элементов
    /// </summary>
    public static DisplacementElement AddElementsToExistingDisplacement(
        Document doc,
        DisplacementElement existingDisplacement,
        ICollection<ElementId> newElementIds)
    {
        if (existingDisplacement == null)
        {
            throw new ArgumentNullException("existingDisplacement",
                "Существующий DisplacementElement не может быть null");
        }

        // Получаем существующие элементы
        ICollection<ElementId> existingElementIds = existingDisplacement.GetDisplacedElementIds();

        // Создаем объединенный список элементов
        List<ElementId> combinedElementIds = new List<ElementId>(existingElementIds);
        foreach (ElementId id in newElementIds)
        {
            if (!combinedElementIds.Contains(id))
            {
                combinedElementIds.Add(id);
            }
        }

        // Получаем вектор смещения из существующего DisplacementElement
        XYZ displacement = existingDisplacement.GetRelativeDisplacement();

        // Получаем владельца (3D вид)
        View ownerView = existingDisplacement.OwnerViewId != ElementId.InvalidElementId
            ? doc.GetElement(existingDisplacement.OwnerViewId) as View
            : null;


        // Удаляем старый DisplacementElement и создаем новый
        DisplacementElement newDisplacement = null;


        try
        {
            // Удаляем старый DisplacementElement
            doc.Delete(existingDisplacement.Id);

            // Создаем новый DisplacementElement с объединенными элементами
            newDisplacement = DisplacementElement.Create(
                doc,
                combinedElementIds,
                displacement,
                ownerView,
                null);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", "Ошибка при добавлении элементов: " + ex.Message);
        }

        return newDisplacement;
    }

// Изменение элементов в существующем DisplacementElement
    public static void UpdateDisplacementElements(Document doc, DisplacementElement displacementElement,
        ICollection<ElementId> newElementIds)
    {
        // Получаем текущие смещённые элементы
        ICollection<ElementId> currentElementIds = displacementElement.GetDisplacedElementIds();

        // Если нужно добавить элементы к существующим, создаём комбинированный список
        ICollection<ElementId> combinedElementIds = new List<ElementId>(currentElementIds);
        foreach (ElementId id in newElementIds)
        {
            if (!combinedElementIds.Contains(id))
            {
                combinedElementIds.Add(id);
            }
        }

        // Устанавливаем новый список элементов
        displacementElement.SetDisplacedElementIds(combinedElementIds);
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
            Reference refPipe1 = _uidoc.Selection.PickObject(ObjectType.Element, selectionFilter,
                statusPrompt);
            return refPipe1;
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

    private FamilyInstance CreateCouplingBetweenPipes(Pipe pipe1, Pipe pipe2, FamilySymbol familySymbol)
    {
        try
        {
            // Получаем незанятые коннекторы труб
            Connector conn1 = GetBestOpenConnector(pipe1, pipe2);
            Connector conn2 = GetBestOpenConnector(pipe2, pipe1);
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

                // Вычисляем вектор между коннекторами
                XYZ connVector = conn2.Origin - conn1.Origin;
                double distance = connVector.GetLength();
                // Нормализуем и используем как направление
                XYZ direction = connVector.Normalize();
                // Убеждаемся, что направление согласуется с одним из коннекторов
                if (direction.DotProduct(conn1.CoordinateSystem.BasisZ) < 0)
                    direction = direction.Negate();
                // Размещаем в истинной средней точке
                XYZ midPoint = conn1.Origin + direction * (distance / 2);
                // Получаем направление системы трубопровода
                XYZ pipeDirection = 0.5 * (conn1.CoordinateSystem.BasisZ - conn2.CoordinateSystem.BasisZ).Normalize();
                FamilyInstance newFamilyInstance = _doc.Create.NewFamilyInstance(midPoint, familySymbol, pipeDirection,
                    pipe1.ReferenceLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                SetDiameterFitting(pipe1, newFamilyInstance);
                // Получение коннекторов фитинга
                ConnectorSet fittingConnectors = newFamilyInstance.MEPModel.ConnectorManager.Connectors;
                List<Connector> connectors = [];
                connectors.AddRange(fittingConnectors.Cast<Connector>());

                if (connectors.Count >= 2)
                {
                    // Находим лучшие пары коннекторов для соединения
                    Tuple<Connector, Connector> pair1 = FindBestConnectorMatch(conn1, connectors);
                    // Удаляем использованный коннектор из списка
                    connectors.Remove(pair1.Item2);
                    // Находим вторую пару
                    Tuple<Connector, Connector> pair2 = FindBestConnectorMatch(conn2, connectors);

                    try
                    {
                        // Соединяем первую пару
                        pair1.Item1.ConnectTo(pair1.Item2);

                        // Соединяем вторую пару
                        pair2.Item1.ConnectTo(pair2.Item2);


                        return newFamilyInstance;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Ошибка соединения",
                            $"Не удалось соединить коннекторы: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка при создании муфты", ex.Message);
            return null;
        }

        return null;
    }


    /// <summary>
    /// Находит наиболее подходящие пары коннекторов для соединения
    /// </summary>
    /// <param name="sourceConnector">Исходный коннектор (от трубы)</param>
    /// <param name="targetConnectors">Список коннекторов фитинга</param>
    /// <returns>Пара наиболее подходящих коннекторов для соединения</returns>
    /// <summary>
    /// Находит наиболее подходящие пары коннекторов для соединения с учетом направления и геометрии
    /// </summary>
    private Tuple<Connector, Connector> FindBestConnectorMatch(Connector sourceConnector,
        List<Connector> targetConnectors)
    {
        // Проверки на null
        if (sourceConnector == null || targetConnectors == null || targetConnectors.Count == 0)
        {
            throw new ArgumentException("Недопустимые параметры для сопоставления коннекторов");
        }

        Connector bestMatch = null;
        double bestScore = double.MaxValue;

        // Получаем направление исходного коннектора
        XYZ sourceDirection = sourceConnector.CoordinateSystem.BasisZ;

        foreach (Connector targetConn in targetConnectors)
        {
            // Исключаем соединения с самим собой
            if (targetConn.Owner.Id == sourceConnector.Owner.Id)
            {
                continue;
            }

            // Вычисляем расстояние между коннекторами
            double distance = sourceConnector.Origin.DistanceTo(targetConn.Origin);

            // Получаем направление целевого коннектора
            XYZ targetDirection = targetConn.CoordinateSystem.BasisZ;

            // Проверяем совместимость направлений (коннекторы должны быть направлены навстречу)
            // Если коннекторы направлены навстречу, то скалярное произведение должно быть отрицательным
            double directionAlignment = sourceDirection.DotProduct(targetDirection);

            // Чем ближе к -1, тем лучше направлены коннекторы навстречу друг другу
            bool isCompatible = directionAlignment < 0;

            // Проверяем совместимость размеров коннекторов
            bool sizesMatch = Math.Abs(sourceConnector.Radius - targetConn.Radius) < 0.001;

            // Вычисляем общую оценку соответствия (меньше = лучше)
            // Даем больший вес направлению, чем расстоянию
            const double directionFactor = 10.0; // Множитель для веса направления
            double score =
                distance + directionFactor *
                (1.0 + directionAlignment); // Прибавляем 1, чтобы значение было положительным

            // Если направления совместимы, размеры совпадают и оценка лучше предыдущей
            if (!isCompatible || !sizesMatch || !(score < bestScore)) continue;
            bestScore = score;
            bestMatch = targetConn;
        }

        // Если не найдено совместимых коннекторов по направлению и размеру,
        // выбираем просто по направлению
        if (bestMatch == null)
        {
            bestScore = double.MaxValue;

            foreach (Connector targetConn in targetConnectors)
            {
                if (targetConn.Owner.Id == sourceConnector.Owner.Id)
                {
                    continue;
                }

                double distance = sourceConnector.Origin.DistanceTo(targetConn.Origin);
                double directionAlignment = sourceDirection.DotProduct(targetConn.CoordinateSystem.BasisZ);
                bool isCompatible = directionAlignment < 0;

                if (isCompatible && distance < bestScore)
                {
                    bestScore = distance;
                    bestMatch = targetConn;
                }
            }
        }

        // Если все еще не найдено совместимых коннекторов,
        // выбираем просто по расстоянию
        if (bestMatch == null && targetConnectors.Count > 0)
        {
            bestMatch = targetConnectors.OrderBy(c => c.Origin.DistanceTo(sourceConnector.Origin)).First();
        }

        return new Tuple<Connector, Connector>(sourceConnector, bestMatch);
    }

    private static void SetDiameterFitting(Pipe pipe1, FamilyInstance familyInstance)
    {
        Parameter pipeDiameterParam = pipe1.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
        if (pipeDiameterParam is not { HasValue: true }) return;
        double pipeDiameter = pipeDiameterParam.AsDouble();
        Parameter fittingDiameterParam = null;
        Parameter param = familyInstance.LookupParameter("Диаметр");
        if (param is { StorageType: StorageType.Double })
        {
            fittingDiameterParam = param;
        }

        if (fittingDiameterParam != null)
        {
            // Устанавливаем диаметр фитинга равный диаметру трубы
            fittingDiameterParam.Set(pipeDiameter);
        }
        else
        {
            // Если не нашли параметр диаметра, выведем предупреждение
            TaskDialog.Show("Предупреждение",
                "Не удалось найти параметр диаметра для муфты. Соединение может не работать.");
        }
    }


    private Connector GetBestOpenConnector(Pipe sourcePipe, Pipe targetPipe)
    {
        // Получаем все коннекторы
        ConnectorSet connectors = sourcePipe.ConnectorManager.Connectors;
        List<Connector> openConnectors = [];
        openConnectors.AddRange(connectors.Cast<Connector>().Where(connector => !connector.IsConnected));

        // Собираем все открытые коннекторы

        switch (openConnectors.Count)
        {
            case 0:
                return null;
            case 1:
                return openConnectors[0];
        }

        // Если у нас несколько открытых коннекторов, выбираем лучший
        // Получаем центральную точку целевой трубы
        XYZ targetCenter = GetPipeCenter(targetPipe);

        // Выбираем коннектор, ближайший к центру целевой трубы
        openConnectors.Sort((a, b) =>
            a.Origin.DistanceTo(targetCenter).CompareTo(
                b.Origin.DistanceTo(targetCenter)));
        return openConnectors[0];
    }

    /// <summary>
    /// Получает центральную точку трубы
    /// </summary>
    private XYZ GetPipeCenter(Pipe pipe)
    {
        if (pipe.Location is not LocationCurve locationCurve)
            return (pipe.Location as LocationPoint)?.Point ?? XYZ.Zero;
        Curve curve = locationCurve.Curve;
        XYZ startPoint = curve.GetEndPoint(0);
        XYZ endPoint = curve.GetEndPoint(1);
        // Вычисляем центральную точку
        return (startPoint + endPoint) * 0.5;
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
    /// Вычисляет расстояние от трубы до точки
    /// </summary>
    private double DistanceFromPipeToPont(Pipe pipe, XYZ point)
    {
        // Получаем геометрию трубы
        LocationCurve location = pipe.Location as LocationCurve;
        if (location == null)
        {
            return double.MaxValue;
        }

        Curve curve = location.Curve;
        return curve.Distance(point);
    }

    /// <summary>
    /// Определяет среднюю трубу между двумя точками по расстоянию
    /// </summary>
    private Pipe DetermineMidPipeByDistance(Pipe pipe1, Pipe pipe2, XYZ point1, XYZ point2)
    {
        // Получаем геометрию труб
        LocationCurve location1 = pipe1.Location as LocationCurve;
        LocationCurve location2 = pipe2.Location as LocationCurve;

        if (location1 == null || location2 == null)
        {
            return null;
        }

        Curve curve1 = location1.Curve;
        Curve curve2 = location2.Curve;

        // Вычисляем средние точки каждой трубы
        XYZ midPoint1 = curve1.Evaluate(0.5, true);
        XYZ midPoint2 = curve2.Evaluate(0.5, true);

        // Вычисляем центральную точку между точками разреза
        XYZ midPointBetweenCuts = (point1 + point2) * 0.5;

        // Вычисляем расстояния от средних точек труб до центральной точки между разрезами
        double distance1 = midPoint1.DistanceTo(midPointBetweenCuts);
        double distance2 = midPoint2.DistanceTo(midPointBetweenCuts);

        // Возвращаем трубу, средняя точка которой ближе к центральной точке между разрезами
        return distance1 < distance2 ? pipe1 : pipe2;
    }
}