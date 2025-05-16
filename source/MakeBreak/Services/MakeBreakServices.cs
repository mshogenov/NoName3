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
            if (selectReference1 == null) return;
            XYZ pick1 = selectReference1.GlobalPoint;
            var selectedElement = _doc.GetElement(selectReference1);
            PipeWrapper originalPipe = GetOriginalPipe(selectedElement, pick1, out var primaryDisplacement);
            // Проверяем, нашли ли мы трубу
            if (originalPipe == null)
            {
                TaskDialog.Show("Ошибка", "Не удалось найти трубу в указанной точке");
                return;
            }
            XYZ breakPt = originalPipe.ProjectPointOntoCurve(pick1, primaryDisplacement);
            Break break1 = new Break(selectReference1, _doc);
            using TransactionGroup tg = new TransactionGroup(_doc, "Сделать разрыв");
            try
            {
                tg.Start();
                using Transaction transaction = new Transaction(_doc, "Вставка первой муфты");
                transaction.Start();
                XYZ pointOnPipe = GetPointOnPipe(break1.TargetPipe.Pipe, break1.BreakPoint);
                ElementId firstSplitPipeId = PlumbingUtils.BreakCurve(_doc, originalPipe.Id, breakPt);
                // Разрезаем трубу в первой точке

                if (firstSplitPipeId == ElementId.InvalidElementId)
                {
                    TaskDialog.Show("Ошибка", "Не удалось разрезать трубу в первой точке.");
                    transaction.RollBack();
                    tg.RollBack();
                    return;
                }

                PipeWrapper firstSplitPipe =
                    new PipeWrapper(_doc.GetElement(firstSplitPipeId) as Pipe); // Вторая часть - новая труба
                // Создаем муфту между первой и второй частью (используя "Разрыв")
                FamilyInstance firstCoupling =
                    CreateCouplingBetweenPipes(break1, firstSplitPipe, familySymbol);
                if (firstCoupling == null)
                {
                    TaskDialog.Show("Предупреждение", "Не удалось создать муфту в первой точке.");
                    transaction.RollBack();
                    tg.RollBack();
                    return;
                }
                
                DisplacementElement displacementCreate1 = null;
                if (selectedElement is DisplacementElement displacement1)
                {
                    // Проверяем, можно ли смещать элементы и не смещены ли они уже
                    List<ElementId> validElementIds = new List<ElementId>();
                
                    if (DisplacementElement.IsAllowedAsDisplacedElement(originalPipe.Pipe) &&
                        !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, originalPipe.Id))
                    {
                        validElementIds.Add(originalPipe.Id);
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
                Break break2 = new Break(refPipe2, _doc);
                XYZ point2 = refPipe2.GlobalPoint;
                // Проверяем минимальное расстояние между точками
                double distanceBetweenPoints = pick1.DistanceTo(point2).ToMillimeters();
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
                PipeWrapper pipeToCut;
                
                // Проверяем, какая из труб после разрезания имеет такой же ElementId или OST_ID как выбранная точка
                if (originalPipe?.Id.Value == refPipe2.ElementId.Value)
                {
                    pipeToCut = originalPipe;
                }
                else if (firstSplitPipe?.Id.Value == refPipe2.ElementId.Value)
                {
                    pipeToCut = firstSplitPipe;
                }
                else
                {
                    // Используем запасной вариант - проверка по расстоянию
                
                    double dist1 = originalPipe.Curve.Distance(originalPoint2);
                    double dist2 = firstSplitPipe.Curve.Distance(originalPoint2);
                    ;
                    pipeToCut = dist1 < dist2 ? originalPipe : firstSplitPipe;
                }
                
                PipeWrapper midPipe = null;
                PipeWrapper thirdPipe = null;
                FamilyInstance secondCoupling = null;
                // Получаем центральную линию трубы
                
                XYZ projectedPointPipeToCut = null;
                if (primaryDisplacement != null)
                {
                    projectedPointPipeToCut = GetProjectedPoint(pipeToCut,
                        originalPoint2 - primaryDisplacement.GetAbsoluteDisplacement());
                }
                else
                {
                    projectedPointPipeToCut = GetProjectedPoint(pipeToCut, originalPoint2);
                }
        
                // Теперь используем спроецированную точку для разрезания
                ElementId thirdPipeId = PlumbingUtils.BreakCurve(_doc, pipeToCut.Id, projectedPointPipeToCut);
                thirdPipe = new PipeWrapper(_doc.GetElement(thirdPipeId) as Pipe);
                // Создаем муфту между разрезанными частями (используя "Разрыв")
                secondCoupling = CreateCouplingBetweenPipes(break2, thirdPipe, familySymbol);
                if (secondCoupling == null)
                {
                    TaskDialog.Show("Предупреждение", "Не удалось создать муфту во второй точке.");
                    trans2.RollBack();
                    tg.RollBack();
                    return;
                }
                
                // Определяем среднюю трубу между двумя точками разреза
                
                if (pipeToCut != null && originalPipe != null && pipeToCut.Id.Equals(originalPipe.Id))
                {
                    midPipe = new PipeWrapper(DetermineMidPipeByDistance(pipeToCut, thirdPipe, pick1, originalPoint2));
                }
                else if (pipeToCut != null && firstSplitPipe != null &&
                         pipeToCut.Id.Equals(firstSplitPipe.Id))
                {
                    midPipe = new PipeWrapper(DetermineMidPipeByDistance(pipeToCut, thirdPipe, pick1, originalPoint2));
                }
                
                SetParameterBreak(midPipe.Pipe);
                trans2.Commit();
                Transaction tr = new Transaction(_doc, "dfdf");
                tr.Start();
                DisplacementElement displacementCreate2 = null;
                if (selectedElement is DisplacementElement displacement)
                {
                    // Проверяем, можно ли смещать элементы и не смещены ли они уже
                    List<ElementId> validElementIds = [];
                    if (DisplacementElement.IsAllowedAsDisplacedElement(originalPipe.Pipe) &&
                        !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, originalPipe.Id))
                    {
                        validElementIds.Add(originalPipe.Id);
                    }
                
                    if (DisplacementElement.IsAllowedAsDisplacedElement(thirdPipe.Pipe) &&
                        !DisplacementElement.IsElementDisplacedInView(Context.ActiveView, thirdPipe.Id))
                    {
                        validElementIds.Add(thirdPipe.Id);
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
                            displacement);
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
                        primaryDisplacement
                    }, primaryDisplacement);
                }
                
                
                tr.Commit();
                tg.Assimilate();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
        }
    }
    public XYZ GetPointOnPipe(Pipe pipe, XYZ approximatePoint)
    {
        try
        {
            // Получаем кривую из местоположения трубы
            LocationCurve locationCurve = pipe.Location as LocationCurve;
            if (locationCurve != null && locationCurve.Curve != null)
            {
                Curve curve = locationCurve.Curve;

                // Проецируем точку на кривую
                IntersectionResult result = curve.Project(approximatePoint);
                if (result != null && result.XYZPoint != null)
                {
                    // Используем непосредственно XYZPoint из результата проекции
                    // Эта точка уже находится на кривой
                    return result.XYZPoint;
                }

                // Если не получилось спроецировать точку, пробуем конечные точки
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                // Проверяем, какая из точек ближе к approximatePoint
                double distToStart = startPoint.DistanceTo(approximatePoint);
                double distToEnd = endPoint.DistanceTo(approximatePoint);

                if (distToStart <= distToEnd)
                {
                    return startPoint;
                }
                else
                {
                    return endPoint;
                }
            }
        }
        catch (Exception ex)
        {
            // Логирование ошибки
            System.Diagnostics.Debug.WriteLine("Ошибка при получении точки на трубе: " + ex.Message);
        }

        // Если не удалось получить точку, возвращаем null
        return null;
    }

// Проверяет, находится ли точка на кривой
private bool IsCurveContainsPoint(Curve curve, XYZ point)
{
    try
    {
        // Проецируем точку обратно на кривую и проверяем расстояние
        IntersectionResult result = curve.Project(point);
        if (result != null)
        {
            // Если расстояние меньше маленькой величины, считаем что точка на кривой
            double tolerance = 0.0001; // настройте по вашим потребностям
            return result.Distance < tolerance;
        }
    }
    catch
    {
        // Игнорируем ошибки и считаем, что точка не на кривой
    }

    return false;
}
    /// <summary>
    /// Собственная реализация разрезания трубы, работающая независимо от ориентации трубы
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="pipeId">ID трубы для разрезания</param>
    /// <param name="point">Точка разрезания</param>
    /// <returns>ID одной из новых труб или InvalidElementId в случае ошибки</returns>
    public ElementId CustomBreakPipe(Document doc, ElementId pipeId, XYZ point)
    {
        // Получаем трубу по ID
        Pipe originalPipe = doc.GetElement(pipeId) as Pipe;
        if (originalPipe == null)
        {
            TaskDialog.Show("Ошибка", "Элемент не является трубой");
            return ElementId.InvalidElementId;
        }

        try
        {
            // Получаем геометрию трубы
            LocationCurve locCrv = originalPipe.Location as LocationCurve;
            if (locCrv == null)
            {
                TaskDialog.Show("Ошибка", "Труба не имеет геометрии кривой");
                return ElementId.InvalidElementId;
            }

            Curve pipeCurve = locCrv.Curve;

            // Получаем начальную и конечную точки трубы
            XYZ startPoint = pipeCurve.GetEndPoint(0);
            XYZ endPoint = pipeCurve.GetEndPoint(1);

            // Проецируем точку на кривую, чтобы гарантировать, что точка разреза находится на трубе
            IntersectionResult result = pipeCurve.Project(point);
            if (result == null)
            {
                TaskDialog.Show("Ошибка", "Не удалось спроецировать точку на трубу");
                return ElementId.InvalidElementId;
            }

            XYZ breakPoint = result.XYZPoint;

            // Убедимся, что точка разреза не слишком близко к концам трубы
            double minDistanceFromEnd = 0.05 * startPoint.DistanceTo(endPoint); // 5% от длины трубы

            if (breakPoint.DistanceTo(startPoint) < minDistanceFromEnd)
            {
                // Если точка слишком близко к началу, сместим ее
                XYZ direction = (endPoint - startPoint).Normalize();
                breakPoint = startPoint + direction * minDistanceFromEnd;
            }
            else if (breakPoint.DistanceTo(endPoint) < minDistanceFromEnd)
            {
                // Если точка слишком близко к концу, сместим ее
                XYZ direction = (endPoint - startPoint).Normalize();
                breakPoint = endPoint - direction * minDistanceFromEnd;
            }

            // Получаем важные параметры исходной трубы
            PipeType pipeType = originalPipe.PipeType;
            ElementId pipeTypeId = pipeType.Id;
            ElementId levelId = originalPipe.ReferenceLevel.Id;
            ElementId systemTypeId = originalPipe.MEPSystem != null ? originalPipe.MEPSystem.GetTypeId() : null;

            // Создаем две новые трубы
            Pipe pipe1 = null;
            Pipe pipe2 = null;

            // Пробуем стандартный метод создания труб
            try
            {
                if (systemTypeId != null)
                {
                    pipe1 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, startPoint, breakPoint);
                    pipe2 = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, breakPoint, endPoint);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Не удалось создать новые трубы: {ex.Message}");
                return ElementId.InvalidElementId;
            }

            if (pipe1 == null || pipe2 == null)
            {
                TaskDialog.Show("Ошибка", "Не удалось создать новые трубы");
                return ElementId.InvalidElementId;
            }

            // Копируем все доступные параметры с исходной трубы на новые трубы
            CopyPipeParameters(originalPipe, pipe1);
            CopyPipeParameters(originalPipe, pipe2);

            // Если у исходной трубы были соединения
            // Создаем новые соединения для новых труб
            // Этот шаг может потребовать более сложной логики в зависимости от вашего проекта

            // Удаляем исходную трубу
            doc.Delete(pipeId);

            // Коннекторы нужно соединить, если есть соседние элементы
            ConnectPipeIfNeeded(doc, pipe1, pipe2);

            // Возвращаем ID одной из новых труб (обычно первой)
            return pipe1.Id;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка в CustomBreakPipe", $"Произошла ошибка: {ex.Message}");
            return ElementId.InvalidElementId;
        }
    }

    /// <summary>
    /// Копирует параметры с исходной трубы на новую
    /// </summary>
    private void CopyPipeParameters(Pipe source, Pipe target)
    {
        try
        {
            // Копируем диаметр
            Parameter sourceDiameter = source.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (sourceDiameter != null && !sourceDiameter.IsReadOnly)
            {
                Parameter targetDiameter = target.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (targetDiameter != null)
                {
                    targetDiameter.Set(sourceDiameter.AsDouble());
                }
            }

            // Копируем уровень смещения
            Parameter sourceOffset = source.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
            if (sourceOffset != null && !sourceOffset.IsReadOnly)
            {
                Parameter targetOffset = target.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                if (targetOffset != null)
                {
                    targetOffset.Set(sourceOffset.AsDouble());
                }
            }


            // Можно добавить копирование других необходимых параметров
        }
        catch (Exception ex)
        {
            // Обрабатываем ошибки копирования параметров
            TaskDialog.Show("Предупреждение", $"Некоторые параметры не были скопированы: {ex.Message}");
        }
    }

    /// <summary>
    /// Соединяет трубы если необходимо
    /// </summary>
    private void ConnectPipeIfNeeded(Document doc, Pipe pipe1, Pipe pipe2)
    {
        try
        {
            // Получаем наборы коннекторов для обеих труб
            ConnectorSet pipe1Connectors = pipe1.ConnectorManager.Connectors;
            ConnectorSet pipe2Connectors = pipe2.ConnectorManager.Connectors;

            // Находим ближайшие коннекторы для соединения
            Connector pipe1EndConnector = null;
            Connector pipe2StartConnector = null;

            foreach (Connector c1 in pipe1Connectors)
            {
                foreach (Connector c2 in pipe2Connectors)
                {
                    if (c1.Origin.DistanceTo(c2.Origin) < 0.001) // Если коннекторы близки
                    {
                        pipe1EndConnector = c1;
                        pipe2StartConnector = c2;
                        break;
                    }
                }

                if (pipe1EndConnector != null)
                    break;
            }

            // Соединяем трубы, если нашли подходящие коннекторы
            if (pipe1EndConnector != null && pipe2StartConnector != null)
            {
                // Используем коннектор для создания соединения
                pipe1EndConnector.ConnectTo(pipe2StartConnector);

                // Или создаем соединение с помощью API соединений
                // doc.Create.NewElbowFitting(pipe1EndConnector, pipe2StartConnector);
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Предупреждение", $"Не удалось соединить трубы: {ex.Message}");
        }
    }

    private PipeWrapper GetOriginalPipe(Element selectedElement, XYZ pick, out DisplacementElement primaryDisplacement)
    {
        PipeWrapper originalPipe = null;
        primaryDisplacement = null;
        switch (selectedElement)
        {
            case Pipe pipe:
                originalPipe = new PipeWrapper(pipe);
                break;
            case DisplacementElement displacementElement:
            {
                primaryDisplacement = displacementElement;
                var displacementElementIds = displacementElement.GetDisplacedElementIds();

                foreach (ElementId displacedId in displacementElementIds)
                {
                    Element element = _doc.GetElement(displacedId);

                    // Проверяем, является ли элемент трубой
                    if (element is not Pipe pipe) continue;
                    // Получаем геометрию трубы
                    BoundingBoxXYZ bounding = pipe.get_BoundingBox(_doc.ActiveView);
                    var contains = bounding.Contains(pick);
                    if (!contains) continue;
                    // Нашли трубу, которая проходит через точку
                    originalPipe = new PipeWrapper(pipe)
                    {
                        IsDisplacement = true
                    };
                    break;
                }

                break;
            }
        }

        return originalPipe;
    }


    private static XYZ GetProjectedPoint(PipeWrapper pipeToCut, XYZ originalPoint2)
    {
        // Проецируем выбранную точку на центральную линию трубы
        IntersectionResult result = pipeToCut.Curve.Project(originalPoint2);
        XYZ projectedPoint = result?.XYZPoint;
        return projectedPoint;
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
                bBreak.PickPoint,
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

    /// <summary>
    /// Соединяет фитинг с двумя трубами через указанные коннекторы
    /// </summary>
    /// <param name="fitting">Фитинг для соединения</param>
    /// <param name="pipeConnector1">Коннектор первой трубы</param>
    /// <param name="pipeConnector2">Коннектор второй трубы</param>
    private void ConnectFittingWithPipes(FamilyInstance fitting, Connector pipeConnector1, Connector pipeConnector2)
    {
        try
        {
            // Получаем коннекторы фитинга
            ConnectorSet fittingConnectors = fitting.MEPModel.ConnectorManager.Connectors;

            // Находим подходящие коннекторы фитинга для соединения с трубами
            Connector fittingConnector1 = null;
            Connector fittingConnector2 = null;

            // Направления коннекторов труб
            XYZ pipeDirection1 = pipeConnector1.CoordinateSystem.BasisZ;
            XYZ pipeDirection2 = pipeConnector2.CoordinateSystem.BasisZ;

            // Находим коннекторы фитинга, которые лучше всего соответствуют направлениям коннекторов труб
            double bestMatch1 = -2.0;
            double bestMatch2 = -2.0;

            foreach (Connector fc in fittingConnectors)
            {
                if (fc.ConnectorType != ConnectorType.End)
                    continue;

                if (fc.IsConnected)
                    continue;

                XYZ fittingDirection = fc.CoordinateSystem.BasisZ;

                // Проверяем соответствие с первым коннектором трубы
                double dotProduct1 = pipeDirection1.DotProduct(fittingDirection);
                if (dotProduct1 > bestMatch1)
                {
                    bestMatch1 = dotProduct1;
                    fittingConnector1 = fc;
                }

                // Проверяем соответствие со вторым коннектором трубы
                double dotProduct2 = pipeDirection2.DotProduct(fittingDirection);
                if (dotProduct2 > bestMatch2)
                {
                    bestMatch2 = dotProduct2;
                    fittingConnector2 = fc;
                }
            }

            // Если нашли подходящие коннекторы, соединяем их
            if (fittingConnector1 != null && !pipeConnector1.IsConnected)
            {
                // Проверяем, что коннекторы находятся достаточно близко друг к другу
                double distance = fittingConnector1.Origin.DistanceTo(pipeConnector1.Origin);
                if (distance < 0.1) // допустимое расстояние в футах
                {
                    // Соединяем коннекторы
                    fittingConnector1.ConnectTo(pipeConnector1);
                }
                else
                {
                    TaskDialog.Show("Предупреждение", $"Коннекторы слишком далеко друг от друга: {distance} футов");
                }
            }

            if (fittingConnector2 != null && !pipeConnector2.IsConnected)
            {
                // Проверяем, что коннекторы находятся достаточно близко друг к другу
                double distance = fittingConnector2.Origin.DistanceTo(pipeConnector2.Origin);
                if (distance < 0.1) // допустимое расстояние в футах
                {
                    // Соединяем коннекторы
                    fittingConnector2.ConnectTo(pipeConnector2);
                }
                else
                {
                    TaskDialog.Show("Предупреждение", $"Коннекторы слишком далеко друг от друга: {distance} футов");
                }
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", $"Ошибка при соединении фитинга с трубами: {ex.Message}");
        }
    }
    private Connector FindBestConnectorMatch(List<Connector> fittingConnectors, Connector pipeConnector)
    {
        // Сортировка коннекторов по сходству направления (противоположные направления лучше)
        return fittingConnectors
            .OrderBy(fc => {
                double dotProduct = fc.CoordinateSystem.BasisZ.DotProduct(pipeConnector.CoordinateSystem.BasisZ);
                // Для противоположных направлений dotProduct ≈ -1, что нам и нужно
                return Math.Abs(dotProduct + 1.0);
            })
            .FirstOrDefault();
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
    public void MergeDisplacementElements(Document doc, IList<DisplacementElement> displacementsToMerge,
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

    public static Connector NearestConnector(Connector[] connectors, XYZ startPoint)
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

    private Connector GetBestOpenConnector(PipeWrapper pipe1, PipeWrapper pipe2)
    {
        List<Connector> openConnectors = pipe1.GetOpenConnectors().ToList();
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
        XYZ targetCenter = pipe2.GetPipeCenter();

        // Выбираем коннектор, ближайший к центру целевой трубы
        openConnectors.Sort((a, b) =>
            a.Origin.DistanceTo(targetCenter).CompareTo(
                b.Origin.DistanceTo(targetCenter)));
        return openConnectors[0];
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
    private double DistanceFromPipeToPont(PipeWrapper pipe, XYZ point)
    {
        return pipe.Curve.Distance(point);
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
}