using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using NoNameApi.Views;
using GeometryObject = Autodesk.Revit.DB.GeometryObject;

namespace RoomsInSpaces.Services;

public class RoomsInSpacesServices
{
    public void RoomsInSpaces(Document doc, RevitLinkInstance linkInstance, List<Room> linkedRooms)
    {
        if (linkInstance == null)
        {
            return;
        }

        Document linkedDoc = linkInstance.GetLinkDocument();
        Transform linkTransform = linkInstance.GetTotalTransform();
        // Собираем все существующие пространства в текущем документе 
        List<Space> existingSpaces = GetSpace(doc).ToList();
        int createdCount = 0;
        int updatedCount = 0;
        using Transaction trans = new(doc, "Импорт пространств из связанного файла");
        trans.Start();
        List<Level> currentLevels = GenerateNecessaryLevels(doc, linkedDoc, linkedRooms);
        var progressBar = new ProgressWindow(linkedRooms.Count);
        progressBar.Show();
        for (int currentIndex = 0; currentIndex < linkedRooms.Count; currentIndex++)
        {
            if (progressBar.IsCancelling)
            {
                trans.RollBack();
                return;
            }

            progressBar.UpdateProgress(currentIndex + 1);
            Room linkedRoom = linkedRooms[currentIndex];
            XYZ roomCenter = GetRoomCenter(linkedRoom);
            if (roomCenter == null)
            {
                TaskDialog.Show("Предупреждение",
                    $"Центр помещения '{linkedRoom.Number}' не определен. Пропуск.");
                return;
            }

            Level linkedRoomLevel = linkedRoom.Level;
            Level currentDocLevel = currentLevels.FirstOrDefault(level =>
                Math.Abs(linkedRoomLevel.Elevation - level.Elevation) < 0.001);
            if (currentDocLevel == null)
            {
                TaskDialog.Show("Предупреждение",
                    $"Уровень '{linkedRoomLevel.Name}' не найден в текущем документе. Пропуск помещения '{linkedRoom.Number}'.");
                return;
            }

            Space intersectedSpace =
                FindIntersectedSpace(existingSpaces, linkedRooms[currentIndex], linkTransform, doc);
            if (intersectedSpace != null)
            {
                bool wasUpdated = UpdateSpaceParameters(intersectedSpace);
                if (wasUpdated)
                    updatedCount++;
            }
            else
            {
                UV point = ConvertRoomCoordinates(linkTransform, linkedRoom);
                if (point == null)
                {
                    TaskDialog.Show("Предупреждение",
                        $"Не удалось определить координаты для помещения '{linkedRoom.Number}'. Пропуск.");
                    return;
                }

                try
                {
                    Space newSpace = doc.Create.NewSpace(currentDocLevel, point);
                    SetSpaceParameters(newSpace, linkedRoom);
                    existingSpaces.Add(newSpace);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Ошибка",
                        $"Не удалось создать пространство для помещения '{linkedRoom.Number}': {ex.Message}");
                }
            }
        }

        trans.Commit();
        TaskDialog.Show("Результат",
            $"Создано пространств: {createdCount}\n" + $"Обновлено пространств: {updatedCount}");
    }

    private bool IsRoomValid(Room room)
    {
        try
        {
            // Проверяем, что помещение не удалено и имеет валидную геометрию
            if (room == null || !room.IsValidObject)
                return false;

            // Проверяем площадь помещения
            double area = room.Area;
            if (area <= 0)
                return false;

            // Проверяем, что помещение ограничено (bounded)
            if (room.Location == null)
                return false;

            // Проверяем уровень помещения
            Level roomLevel = room.Level;
            if (roomLevel == null)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private Space FindIntersectedSpace(List<Space> spaces, Room room, Transform linkTransform, Document doc)
    {
        // Проверяем валидность помещения
        if (!IsRoomValid(room))
            return null;

        // 1. Быстрая проверка по ограничивающим прямоугольникам
        BoundingBoxXYZ roomBBox = room.get_BoundingBox(null);
        if (roomBBox == null) return null;

        BoundingBoxXYZ transformedRoomBBox = new BoundingBoxXYZ();
        transformedRoomBBox.Min = linkTransform.OfPoint(roomBBox.Min);
        transformedRoomBBox.Max = linkTransform.OfPoint(roomBBox.Max);

        List<Space> potentialSpaces = new List<Space>();
        foreach (Space space in spaces)
        {
            BoundingBoxXYZ spaceBBox = space.get_BoundingBox(null);
            if (spaceBBox == null) continue;

            if (DoBoxesIntersect(transformedRoomBBox, spaceBBox))
            {
                potentialSpaces.Add(space);
            }
        }

        if (potentialSpaces.Count == 0) return null;

        // 2. Получаем геометрию помещения с улучшенной обработкой ошибок
        Solid roomSolid = GetRoomSolid(room, room.Document);
        if (roomSolid == null)
            return null;

        // Трансформируем геометрию
        try
        {
            roomSolid = SolidUtils.CreateTransformed(roomSolid, linkTransform);
        }
        catch
        {
            return null;
        }

        // 3. Ищем лучшее совпадение
        SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(doc);
        Space bestMatch = null;
        double maxIntersectionRatio = 0.05;

        foreach (Space space in potentialSpaces)
        {
            try
            {
                SpatialElementGeometryResults spaceResults = calculator.CalculateSpatialElementGeometry(space);
                Solid spaceSolid = spaceResults.GetGeometry();

                if (spaceSolid == null || spaceSolid.Volume < 0.001) continue;

                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                    roomSolid, spaceSolid, BooleanOperationsType.Intersect);

                if (intersection != null && intersection.Volume > 0.001)
                {
                    double intersectionRatio = intersection.Volume / roomSolid.Volume;

                    if (intersectionRatio > maxIntersectionRatio)
                    {
                        maxIntersectionRatio = intersectionRatio;
                        bestMatch = space;

                        if (intersectionRatio > 0.9) return space;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        return bestMatch;
    }

    private Solid GetRoomSolid(Room room, Document doc)
    {
        try
        {
            // Метод 1: Стандартный калькулятор
            SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(doc);
            SpatialElementGeometryResults roomResults = calculator.CalculateSpatialElementGeometry(room);
            Solid roomSolid = roomResults.GetGeometry();

            if (roomSolid != null && roomSolid.Volume > 0.001)
                return roomSolid;
        }
        catch
        {
        }

        try
        {
            // Метод 2: Получение через Options
            Options options = new Options();
            options.ComputeReferences = false;
            options.DetailLevel = ViewDetailLevel.Coarse;
            options.IncludeNonVisibleObjects = false;

            GeometryElement geomElement = room.get_Geometry(options);
            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is Solid solid && solid.Volume > 0.001)
                        return solid;
                }
            }
        }
        catch
        {
        }

        try
        {
            // Метод 3: Создание геометрии на основе границ помещения
            return CreateSolidFromRoomBoundaries(room);
        }
        catch
        {
        }

        return null;
    }

    private Solid CreateSolidFromRoomBoundaries(Room room)
    {
        try
        {
            // Получаем границы помещения
            IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

            if (boundaries == null || boundaries.Count == 0)
                return null;

            List<CurveLoop> curveLoops = new List<CurveLoop>();

            foreach (IList<BoundarySegment> boundary in boundaries)
            {
                CurveLoop curveLoop = new CurveLoop();
                foreach (BoundarySegment segment in boundary)
                {
                    Curve curve = segment.GetCurve();
                    if (curve != null)
                        curveLoop.Append(curve);
                }

                if (curveLoop.NumberOfCurves() > 0)
                    curveLoops.Add(curveLoop);
            }

            if (curveLoops.Count > 0)
            {
                // Создаем твердое тело экструзией
                double height = GetRoomHeight(room);
                XYZ extrusionDirection = XYZ.BasisZ;

                return GeometryCreationUtilities.CreateExtrusionGeometry(
                    curveLoops, extrusionDirection, height);
            }
        }
        catch
        {
        }

        return null;
    }

    private double GetRoomHeight(Room room)
    {
        try
        {
            // Пытаемся получить высоту из параметров
            Parameter heightParam = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
            if (heightParam != null && heightParam.HasValue)
                return heightParam.AsDouble();

            // Альтернативно - используем высоту уровня
            Level level = room.Level;
            if (level != null)
            {
                // Возвращаем стандартную высоту помещения (например, 3 метра)
                return UnitUtils.ConvertToInternalUnits(3.0, UnitTypeId.Meters);
            }
        }
        catch
        {
        }

        // Высота по умолчанию
        return UnitUtils.ConvertToInternalUnits(2.7, UnitTypeId.Meters);
    }

// Вспомогательный метод для проверки пересечения ограничивающих прямоугольников
    private bool DoBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        // Проверка по x, y, z
        return (box1.Min.X <= box2.Max.X && box1.Max.X >= box2.Min.X) &&
               (box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y) &&
               (box1.Min.Z <= box2.Max.Z && box1.Max.Z >= box2.Min.Z);
    }

    private BoundingBoxXYZ CreateBoundingBoxAtPoint(
        XYZ centerPoint,
        double halfWidthX,
        double halfWidthY,
        double halfHeightZ)
    {
        XYZ minPoint = new XYZ(
            centerPoint.X - halfWidthX,
            centerPoint.Y - halfWidthY,
            centerPoint.Z - halfHeightZ);

        XYZ maxPoint = new XYZ(
            centerPoint.X + halfWidthX,
            centerPoint.Y + halfWidthY,
            centerPoint.Z + halfHeightZ);

        BoundingBoxXYZ bbox = new BoundingBoxXYZ();
        bbox.Min = minPoint;
        bbox.Max = maxPoint;

        return bbox;
    }

    private XYZ GetRoomCenter(Room room)
    {
        GeometryElement geomElement = room.get_Geometry(new Options());
        if (geomElement != null)
        {
            foreach (GeometryObject geomObj in geomElement)
            {
                Solid solid = geomObj as Solid;
                if (solid != null && solid.Volume > 1.0e-6)
                {
                    // Получаем центр через вычисление центроида
                    return solid.ComputeCentroid();
                }
            }
        }

        // Если и это не сработало, используем BoundingBox
        BoundingBoxXYZ bbox = room.get_BoundingBox(null);
        if (bbox != null)
        {
            return (bbox.Max + bbox.Min) / 2;
        }

        return null;
    }

    /// <summary>
    /// Проверяем, есть ли среди имеющихся Spaces на данном уровне такое,
    /// чья bounding box пересекается с bounding box помещения.
    /// Если пересечений несколько, берём ближайшее по расстоянию между центрами.
    /// </summary>
    private Space FindIntersectedSpace(List<Space> spaces, BoundingBoxXYZ roomBb)
    {
        XYZ roomCenter = (roomBb.Max + roomBb.Min) / 2;

        Space nearestSpace = null;
        double minDistance = double.MaxValue;
        const double buffer = 0.05;
        foreach (Space space in spaces)
        {
            BoundingBoxXYZ spaceBb = space.get_BoundingBox(null);
            if (spaceBb == null) continue;

            // Проверяем что комната полностью внутри пространства с учетом отступа
            if (!IsRoomInsideSpace(roomBb, spaceBb, buffer)) continue;
            XYZ spaceCenter = (spaceBb.Max + spaceBb.Min) / 2;
            double distance = roomCenter.DistanceTo(spaceCenter);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestSpace = space;
            }
        }

        return nearestSpace;
    }

    /// <summary>
    /// Проверяет, пересекаются ли два ограничивающих прямоугольника
    /// </summary>
    private bool DoBoundingBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        // Проверяем, что один из прямоугольников не находится полностью справа, слева, выше или ниже другого
        return !(
            box1.Min.X > box2.Max.X ||
            box1.Max.X < box2.Min.X ||
            box1.Min.Y > box2.Max.Y ||
            box1.Max.Y < box2.Min.Y ||
            box1.Min.Z > box2.Max.Z ||
            box1.Max.Z < box2.Min.Z
        );
    }

    private bool IsRoomInsideSpace(BoundingBoxXYZ roomBb, BoundingBoxXYZ spaceBb, double buffer)
    {
        // Проверяем, что помещение находится внутри пространства по всем осям с учетом отступа
        return (roomBb.Min.X + buffer >= spaceBb.Min.X && roomBb.Max.X - buffer <= spaceBb.Max.X) &&
               (roomBb.Min.Y + buffer >= spaceBb.Min.Y && roomBb.Max.Y - buffer <= spaceBb.Max.Y) &&
               (roomBb.Min.Z + buffer >= spaceBb.Min.Z && roomBb.Max.Z - buffer <= spaceBb.Max.Z);
    }

    /// <summary>
    /// Копируем имя и номер из помещения (Room) в пространство (Space).
    /// Возвращаем true, если какие-то значения реально изменились.
    /// </summary>
    private bool UpdateSpaceParameters(Space space)
    {
        bool updated = false;

        string roomName = space.get_Parameter(BuiltInParameter.SPACE_ASSOC_ROOM_NAME).AsValueString();
        string roomNumber = space.get_Parameter(BuiltInParameter.SPACE_ASSOC_ROOM_NUMBER).AsValueString();
        // Получаем параметры пространства
        Parameter spaceNameParam = space.get_Parameter(BuiltInParameter.ROOM_NAME);
        Parameter spaceNumberParam = space.get_Parameter(BuiltInParameter.ROOM_NUMBER);

        if (spaceNameParam == null || spaceNumberParam == null)
        {
            return false;
        }

        // Обновляем только если значения не пустые
        if (!string.IsNullOrEmpty(roomName) && roomName != spaceNameParam.AsValueString())
        {
            spaceNameParam.Set(roomName);
            updated = true;
        }

        if (!string.IsNullOrEmpty(roomNumber) && roomNumber != spaceNumberParam.AsValueString())
        {
            spaceNumberParam.Set(roomNumber);
            updated = true;
        }

        return updated;
    }

    private void SetSpaceParameters(Space space, Room room)
    {
        string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsValueString() ?? "";
        string roomNumber = room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsValueString() ?? "";
        Parameter spaceNameParam = space.get_Parameter(BuiltInParameter.ROOM_NAME);
        Parameter spaceNumberParam = space.get_Parameter(BuiltInParameter.ROOM_NUMBER);
        if (spaceNameParam.AsValueString() != roomName)
        {
            spaceNameParam.Set(roomName);
        }

        if (spaceNumberParam.AsValueString() == roomNumber) return;
        spaceNumberParam.Set(roomNumber);
    }

    /// <summary>
    /// Проверяет на наличие необходимых уровней из связанного файла в текущем файле, затем создает отсутствующие уровни в текущем файле 
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="linkedDoc"></param>
    /// <param name="linkedRooms"></param>
    /// <returns></returns>
    private List<Level> GenerateNecessaryLevels(Document doc, Document linkedDoc, IEnumerable<Room> linkedRooms)
    {
        //Получение используемых уровней для помещений
        IEnumerable<Level> uniqueLevels = GetRoomLevelsInUse(linkedDoc, linkedRooms);
        var currentLevels = GetLevels(doc);
        var missingLevels = uniqueLevels
            .Where(l => !currentLevels.Select(x => Math.Round(x.Elevation, 2))
                .Contains(Math.Round(l.Elevation, 2)));
        {
            foreach (var missingLevel in missingLevels)
            {
                Level newLevel = Level.Create(doc, missingLevel.Elevation);
                newLevel.Name = missingLevel.Name;
                currentLevels.Add(newLevel); // Добавляем новый уровень в текущий список уровней
            }
        }
        return currentLevels;
    }

    /// <summary>
    /// Преобразует координаты помещения из связанного файла для совпадения с текущим файлом
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="linkedRoom"></param>
    /// <returns></returns>
    private UV ConvertRoomCoordinates(Transform transform, Room linkedRoom)
    {
        if (linkedRoom.Location is not LocationPoint roomLocationPoint) return null;
        XYZ roomPointInLinked = roomLocationPoint.Point;
        XYZ roomPointInHost = transform.OfPoint(roomPointInLinked);
        //Получение геометрии помещения
        UV uV = new(roomPointInHost.X, roomPointInHost.Y);
        return uV;
    }

    /// <summary>
    /// Получает все пространства в документе
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    private IEnumerable<Space> GetSpace(Document doc)
    {
        if (doc != null)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement))
                .WhereElementIsNotElementType()
                .Where(x => x is Space)
                .Cast<Space>();
        }

        return null;
    }

    /// <summary>
    /// Получает только используемые уровни помещений
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="rooms"></param>
    /// <returns></returns>
    private IEnumerable<Level> GetRoomLevelsInUse(Document doc, IEnumerable<Room> rooms)
    {
        HashSet<ElementId> uniqueLevelIds = new(rooms.Select(room => room.LevelId));
        var uniqueLevels = uniqueLevelIds
            .Select(id => doc.GetElement(id) as Level)
            .Where(level => level != null);
        return uniqueLevels;
    }

    /// <summary>
    /// Получает все помещения из документа
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>Возвращает список помещений</returns>
    public IEnumerable<Room> GetRooms(Document doc)
    {
        if (doc != null)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0);
        }

        return null;
    }

    /// <summary>
    /// Получает все уровни в документе
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    private List<Level> GetLevels(Document doc)
    {
        // Создаем фильтр для получения всех уровней в документе
        FilteredElementCollector collector = new(doc);
        ICollection<Element> levels = collector.OfClass(typeof(Level)).ToElements();

        // Преобразуем ICollection в список уровней
        List<Level> levelList = [];
        levelList.AddRange(levels.Select(level => level as Level));
        return levelList;
    }
}