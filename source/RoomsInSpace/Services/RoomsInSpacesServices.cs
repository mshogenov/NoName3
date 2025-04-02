using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using NoNameApi.Views;
using GeometryObject = Autodesk.Revit.DB.GeometryObject;

namespace RoomsInSpaces.Services;

public class RoomsInSpacesServices
{
    public void RoomsInSpaces(Document doc, RevitLinkInstance linkInstance)
    {
        if (linkInstance == null)
        {
            return;
        }

        Document linkedDoc = linkInstance.GetLinkDocument();
        Transform linkTransform = linkInstance.GetTotalTransform();
        // Получение помещений из связанного документа
        var linkedRooms = GetRooms(linkedDoc).ToList();
        // Собираем все существующие пространства в текущем документе 
        List<Space> existingSpaces = GetSpace(doc).ToList();
        List<Level> currentLevels = GenerateNecessaryLevels(doc, linkedDoc, linkedRooms);
        int createdCount = 0;
        int updatedCount = 0;
        using Transaction trans = new(doc, "Импорт пространств из связанного файла");
        trans.Start();
        using var progressBar = new ProgressWindow(linkedRooms.Count);
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
                    continue;
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
                    trans.RollBack();
                    TaskDialog.Show("Ошибка",
                        $"Не удалось создать пространство для помещения '{linkedRoom.Number}': {ex.Message}");
                    return;
                }
            }
        }

        trans.Commit();
        TaskDialog.Show("Результат",
            $"Создано пространств: {createdCount}\n" + $"Обновлено пространств: {updatedCount}");
    }

    private Space FindIntersectedSpace(List<Space> spaces, Room room, Transform linkTransform, Document doc)
    {
        // Создаем калькулятор геометрии пространственных элементов
        SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(doc);

        // Получаем геометрию помещения из связанного файла
        SpatialElementGeometryResults roomResults;
        try
        {
            roomResults = calculator.CalculateSpatialElementGeometry(room);
        }
        catch
        {
            return null; // Если не получилось вычислить геометрию
        }

        Solid roomSolid = roomResults.GetGeometry();
        if (roomSolid == null) return null;

        // Трансформируем геометрию помещения в координаты текущего документа
        roomSolid = SolidUtils.CreateTransformed(roomSolid, linkTransform);

        // Проверяем каждое пространство на пересечение
        foreach (Space space in spaces)
        {
            try
            {
                SpatialElementGeometryResults spaceResults = calculator.CalculateSpatialElementGeometry(space);
                Solid spaceSolid = spaceResults.GetGeometry();

                if (spaceSolid == null) continue;

                // Вычисляем пересечение объемов
                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                    roomSolid, spaceSolid, BooleanOperationsType.Intersect);

                if (intersection != null && intersection.Volume > 0)
                {
                    // Можно добавить проверку на минимальный объем пересечения
                    // Например, если intersection.Volume / roomSolid.Volume > 0.5
                    return space;
                }
            }
            catch
            {
                continue; // Если не получилось вычислить геометрию для этого пространства
            }
        }

        return null;
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

        // Получаем связанное помещение
        Room associatedRoom = space.Room;
        if (associatedRoom == null)
        {
            return false;
        }

        // Получаем имя и номер из помещения
        string roomName = associatedRoom.Name;
        string roomNumber = associatedRoom.Number;

        // Получаем параметры пространства
        Parameter spaceNameParam = space.get_Parameter(BuiltInParameter.ROOM_NAME);
        Parameter spaceNumberParam = space.get_Parameter(BuiltInParameter.ROOM_NUMBER);

        if (spaceNameParam == null || spaceNumberParam == null)
        {
            return false;
        }

        // Обновляем только если значения не пустые
        if (!string.IsNullOrEmpty(roomName))
        {
            spaceNameParam.Set(roomName);
            updated = true;
        }

        if (!string.IsNullOrEmpty(roomNumber))
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
    private IEnumerable<Room> GetRooms(Document doc)
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