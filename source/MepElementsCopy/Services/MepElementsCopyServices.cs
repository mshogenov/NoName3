using System.Collections;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MepElementsCopy.Models;
using NoNameApi.Views;

namespace MepElementsCopy.Services;

public class MepElementsCopyServices
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly Options _options;


    public MepElementsCopyServices()
    {
        _options = new Options()
        {
            IncludeNonVisibleObjects = false,
            DetailLevel = ViewDetailLevel.Fine
        };
    }

    public IList<Element> GetSelectedElements(UIDocument uiDoc)
    {
        IList<Element> elements = [];
        var elementIds = uiDoc.Selection.GetElementIds().ToList();
        if (elementIds.Count == 0) return elements;
        foreach (var elementId in elementIds)
        {
            elements.Add(uiDoc.Document.GetElement(elementId));
        }

        return elements;
    }

    public IList<Element> SelectedElements(UIDocument uiDoc)
    {
        IList<Element> elements = [];
        IList<Reference> selectedRefs = uiDoc.Selection.PickObjects(ObjectType.Element,
            "Выберите MEP элементы для копирования");
        if (selectedRefs == null) return elements;
        var elementIds = selectedRefs.Select(r => uiDoc.Document?.GetElement(r.ElementId).Id).ToList();
        foreach (var elementId in elementIds)
        {
            elements.Add(uiDoc.Document.GetElement(elementId));
        }

        return elements;
    }

    public void CopyMepElementsToLevel(LevelModel level, List<ElementWrp> mepElementModels,
        List<MepCurveWrp> mepCurveModels)
    {
        List<ElementId> elementIds = mepElementModels
            .Select(x => x.Id)
            .ToList();
        Level bindingLevel = FindMostUsedLevelInSelection(elementIds, _doc);
        if (bindingLevel == null) return;
        double offset = level.Elevation - bindingLevel.Elevation;
        CopyingMepElementsAndConnect(offset, mepElementModels, mepCurveModels);
    }

    public Level FindMostUsedLevelInSelection(List<ElementId> elementIds, Document doc)
    {
        if (elementIds.Count == 0)
        {
            return null;
        }

        // Словарь для подсчета, сколько раз каждый уровень используется
        Dictionary<ElementId, int> levelUsageCount = new Dictionary<ElementId, int>();

        // Проходим по всем выбранным элементам
        foreach (ElementId id in elementIds)
        {
            Element elem = doc.GetElement(id);

            // Пытаемся получить уровень элемента
            ElementId levelId = GetElementLevel(elem);

            if (levelId != null && levelId != ElementId.InvalidElementId)
            {
                // Увеличиваем счетчик использования этого уровня
                if (levelUsageCount.ContainsKey(levelId))
                {
                    levelUsageCount[levelId]++;
                }
                else
                {
                    levelUsageCount[levelId] = 1;
                }
            }
        }

        // Если не найдено ни одного уровня
        if (levelUsageCount.Count == 0)
        {
            return null;
        }

        // Находим уровень с наибольшим количеством использований
        ElementId mostUsedLevelId = levelUsageCount.OrderByDescending(x => x.Value).First().Key;

        // Возвращаем сам уровень
        return doc.GetElement(mostUsedLevelId) as Level;
    }

    /// <summary>
    /// Получает ElementId уровня, связанного с элементом
    /// </summary>
    /// <param name="elem">Элемент для проверки</param>
    /// <returns>ElementId уровня или InvalidElementId, если уровень не найден</returns>
    private ElementId GetElementLevel(Element elem)
    {
        // Проверка на null
        if (elem == null)
            return ElementId.InvalidElementId;

        // Проверяем, есть ли у элемента параметр LevelId
        Parameter levelParam = elem switch
        {
            FamilyInstance => elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM),
            MEPCurve => elem.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM),
            _ => null
        };

        if (levelParam is { HasValue: true })
        {
            return levelParam.AsElementId();
        }

        // Если не удалось определить уровень
        return ElementId.InvalidElementId;
    }

    public void SetBaseLevel(List<ElementWrp> mepElementModels, LevelModel selectedLevel)
    {
        var selectedLevelId = selectedLevel.Id;
        Document doc = mepElementModels.FirstOrDefault()?.Element?.Document;

        if (doc == null || selectedLevel.Level == null)
            return;

        // Получаем elevation выбранного уровня
        double newLevelElevation = selectedLevel.Level.Elevation;

        foreach (var elem in mepElementModels)
        {
            // Получаем текущие координаты элемента
            XYZ currentPosition = GetElementPosition(elem.Element);
            if (currentPosition == null)
                continue;

            // Получаем текущий уровень элемента
            ElementId currentLevelId = null;
            Parameter baseLevelParam = null;
            Parameter offsetParam = null;

            switch (elem.Element)
            {
                case FamilyInstance fi:
                    baseLevelParam = fi.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                    offsetParam = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                    break;

                case MEPCurve curve:
                    baseLevelParam = curve.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                    offsetParam = curve.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                    break;
            }

            if (baseLevelParam == null || offsetParam == null || baseLevelParam.IsReadOnly)
                continue;

            currentLevelId = baseLevelParam.AsElementId();
            if (currentLevelId.Value < 0 || currentLevelId.Equals(selectedLevelId))
                continue;

            // Получаем elevation текущего уровня
            Level currentLevel = doc.GetElement(currentLevelId) as Level;
            if (currentLevel == null)
                continue;

            double currentLevelElevation = currentLevel.Elevation;

            // Получаем текущее смещение от уровня
            double currentOffset = offsetParam.AsDouble();

            // Вычисляем абсолютную высоту элемента
            double absoluteElevation = currentLevelElevation + currentOffset;

            // Вычисляем новое смещение от нового уровня
            double newOffset = absoluteElevation - newLevelElevation;

            // Устанавливаем новый уровень и смещение
            baseLevelParam.Set(selectedLevelId);
            offsetParam.Set(newOffset);
        }
    }

// Вспомогательный метод для получения позиции элемента
    public XYZ GetElementPosition(Element element)
    {
        if (element == null)
            return null;

        switch (element)
        {
            case FamilyInstance fi:
                return fi.Location is LocationPoint locationPoint ? locationPoint.Point : null;

            case MEPCurve mepCurve:
                if (mepCurve.Location is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;
                    return curve?.GetEndPoint(0); // Берем начальную точку кривой
                }

                return null;

            default:
                return null;
        }
    }

    public void SetBaseLevel(Element element, List<Level> levels)
    {
        // Находим ближайший нижний уровень
        Level nearestLevelBelow =
            FindNearestLevelBelow(element, levels);
        if (nearestLevelBelow == null) return;
        var nearestLevelBelowId = nearestLevelBelow.Id;
        // Получаем elevation выбранного уровня
        double newLevelElevation = nearestLevelBelow.Elevation;
        // Получаем текущие координаты элемента
        XYZ currentPosition = GetElementPosition(element);
        if (currentPosition == null)
            return;
        // Получаем текущий уровень элемента
        ElementId currentLevelId = null;
        Parameter baseLevelParam = null;
        Parameter offsetParam = null;
        switch (element)
        {
            case FamilyInstance fi:
                baseLevelParam = fi.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                offsetParam = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                break;

            case MEPCurve curve:
                baseLevelParam = curve.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                offsetParam = curve.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                break;
        }

        if (baseLevelParam == null || offsetParam == null || baseLevelParam.IsReadOnly)
            return;
        currentLevelId = baseLevelParam.AsElementId();
        if (currentLevelId.Value < 0 || currentLevelId.Equals(nearestLevelBelowId))
            return;
        // Получаем elevation текущего уровня
        Level currentLevel = _doc.GetElement(currentLevelId) as Level;
        if (currentLevel == null)
            return;
        double currentLevelElevation = currentLevel.Elevation;

        // Получаем текущее смещение от уровня
        double currentOffset = offsetParam.AsDouble();

        // Вычисляем абсолютную высоту элемента
        double absoluteElevation = currentLevelElevation + currentOffset;

        // Вычисляем новое смещение от нового уровня
        double newOffset = absoluteElevation - newLevelElevation;
        // Устанавливаем базовый уровень
        baseLevelParam.Set(nearestLevelBelowId);
        offsetParam.Set(newOffset);
    }

    public Level FindNearestLevelBelow(Element element, List<Level> sortedLevels, double maxDistanceAbove = 300)
    {
        double elementZ = GetElementZPosition(element);

        if (double.IsNaN(elementZ))
            return null;

        // Конвертируем миллиметры в футы (Revit использует футы)
        double maxDistanceInFeet = maxDistanceAbove / 304.8;

        // Находим ближайший нижний уровень
        Level nearestLevelBelow = null;
        Level levelAbove = null;

        // Проверяем все уровни
        foreach (Level level in sortedLevels)
        {
            // Если уровень ниже или равен элементу
            if (level.Elevation <= elementZ)
            {
                nearestLevelBelow = level;
            }
            // Если уровень выше элемента
            else
            {
                // Проверяем, находится ли уровень на указанном расстоянии или меньше над элементом
                if (level.Elevation - elementZ <= maxDistanceInFeet)
                {
                    levelAbove = level;
                }

                break;
            }
        }

        // Если есть уровень в пределах указанного расстояния над элементом, возвращаем его
        if (levelAbove != null)
        {
            return levelAbove;
        }

        // Если не найден нижний уровень, берем первый (самый нижний) уровень из списка
        if (nearestLevelBelow == null && sortedLevels.Count > 0)
        {
            // Возвращаем ближайший верхний уровень (первый уровень, выше элемента)
            foreach (Level level in sortedLevels)
            {
                if (level.Elevation > elementZ)
                {
                    return level; // Возвращаем первый найденный уровень выше элемента
                }
            }

            // Если ни один уровень не выше элемента, возвращаем самый нижний уровень проекта
            return sortedLevels.First();
        }

        return nearestLevelBelow;
    }

    private double GetElementZPosition(Element element)
    {
        try
        {
            // Для разных типов элементов используем разные методы получения положения
            if (element.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point.Z;
            }
            else if (element.Location is LocationCurve locationCurve)
            {
                XYZ startPoint = locationCurve.Curve.GetEndPoint(0);
                XYZ endPoint = locationCurve.Curve.GetEndPoint(1);
                return (startPoint.Z + endPoint.Z) / 2;
            }
            else if (element.get_BoundingBox(null) != null)
            {
                return element.get_BoundingBox(null).Min.Z;
            }
        }
        catch (Exception)
        {
            // Игнорируем ошибки при получении положения
        }

        return double.NaN;
    }

    internal XYZ FindFurthestPoint(List<ElementWrp> elements, XYZ fromPoint)
    {
        XYZ furthestPoint = null;
        double minValue = double.MinValue;
        foreach (var geomObj in elements.SelectMany(element => element.Element.get_Geometry(_options).ToList()))
        {
            ProcessGeometryObject(geomObj, fromPoint, ref furthestPoint, ref minValue);
        }

        return furthestPoint;
    }

    private void ProcessGeometryObject(GeometryObject geomObj, XYZ fromPoint, ref XYZ furthestPoint,
        ref double maxDistance)
    {
        switch (geomObj)
        {
            case Solid solid:
                IEnumerator enumerator1 = solid.Edges.GetEnumerator();
                try
                {
                    while (enumerator1.MoveNext())
                    {
                        Edge current = (Edge)enumerator1.Current;
                        EvaluatePoint(current?.AsCurve().GetEndPoint(0), fromPoint, ref furthestPoint,
                            ref maxDistance);
                        EvaluatePoint(current?.AsCurve().GetEndPoint(1), fromPoint, ref furthestPoint,
                            ref maxDistance);
                    }

                    break;
                }
                finally
                {
                    if (enumerator1 is IDisposable disposable)
                        disposable.Dispose();
                }
            case Curve curve:
                if (!curve.IsBound)
                    break;
                EvaluatePoint(curve.GetEndPoint(0), fromPoint, ref furthestPoint, ref maxDistance);
                EvaluatePoint(curve.GetEndPoint(1), fromPoint, ref furthestPoint, ref maxDistance);
                break;
            case GeometryInstance geometryInstance:
                using (IEnumerator<GeometryObject> enumerator2 = geometryInstance.GetInstanceGeometry().GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                        ProcessGeometryObject(enumerator2.Current, fromPoint, ref furthestPoint, ref maxDistance);
                    break;
                }
        }
    }

    private void EvaluatePoint(XYZ point, XYZ fromPoint, ref XYZ furthestPoint, ref double maxDistance)
    {
        double num = point.DistanceTo(fromPoint);
        if (num <= maxDistance)
            return;
        maxDistance = num;
        furthestPoint = point;
    }

    private void CopyingMepElementsAndConnect(double offset, List<ElementWrp> mepElementModels,
        List<MepCurveWrp> mepCurveModels, XYZ direction = null)
    {
        XYZ translation = (direction ?? XYZ.BasisZ).Multiply(offset);
        ICollection<ElementId> mepElementsIds =
            ElementTransformUtils.CopyElements(_doc, mepElementModels.Select(m => m.Id).ToList(), translation);
        List<Tuple<ConnectorSplitModel, ConnectorSplitModel>> splitConnectors =
            GetSplitConnectors(mepElementsIds, mepCurveModels);
        if (splitConnectors != null)
            ConnectInMepCurves(splitConnectors, mepCurveModels);
    }

    private void ConnectInMepCurves(List<Tuple<ConnectorSplitModel, ConnectorSplitModel>> splitConnectorsPairs,
        List<MepCurveWrp> mepCurveModels)
    {
        foreach (Tuple<ConnectorSplitModel, ConnectorSplitModel> splitConnectorsPair in splitConnectorsPairs)
        {
            splitConnectorsPair.Deconstruct(out var connectorSplitWr1,
                out var connectorSplitWr2);
            MepCurveWrp existingMepCurve =
                mepCurveModels.FirstOrDefault(m => m.Id == connectorSplitWr1.IdMepCurve);
            if (existingMepCurve == null) continue;
            Connector opositeConnector = existingMepCurve.GetConnectorByDirection(connectorSplitWr2.Direction);
            Connector connectorByDirection = existingMepCurve.GetConnectorByDirection(connectorSplitWr1.Direction);
            XYZ nearestEndPoint1 = existingMepCurve.GetNearestEndPoint(opositeConnector.Origin);
            XYZ nearestEndPoint2 = existingMepCurve.GetNearestEndPoint(connectorByDirection.Origin);
            Connector connector = opositeConnector.AllRefs.OfType<Connector>()
                .FirstOrDefault(c =>
                    c.Owner.Id != opositeConnector.Owner.Id && c.ConnectorType == ConnectorType.End);
            existingMepCurve.StretchToPoint(nearestEndPoint2, connectorSplitWr1.Connector.Origin);
            connectorSplitWr1.Connector.ConnectTo(opositeConnector);
            MEPCurve curve = CreateCurve(connectorSplitWr2.Connector.Origin, nearestEndPoint1, existingMepCurve);
            if (curve == null) continue;
            MepCurveWrp mepCurveWr = new MepCurveWrp(curve);
            mepCurveModels.Add(mepCurveWr);
            if (connector != null)
                mepCurveWr.GetConnectorByDirection(connectorSplitWr2.Direction).ConnectTo(connector);
            connectorSplitWr2.Connector.ConnectTo(mepCurveWr.GetConnectorByDirection(connectorSplitWr1.Direction));
        }
    }

    private MEPCurve CreateCurve(XYZ p1, XYZ p2, MepCurveWrp mWr)
    {
        if (p1.DistanceTo(p2) < 0.1)
            return null;
        ElementId id = ElementTransformUtils.CopyElement(_doc, mWr.Id, XYZ.Zero).FirstOrDefault();
        MEPCurve element = null;
        int num;
        if (!(id == null))
        {
            element = _doc.GetElement(id) as MEPCurve;
            num = element == null ? 1 : 0;
        }
        else
            num = 1;

        if (num != 0)
            return null;
        if (element == null) return null;
        Curve curve = ((LocationCurve)element.Location).Curve;
        XYZ endpoint1 = p2.DistanceTo(curve.GetEndPoint(0)) > p2.DistanceTo(curve.GetEndPoint(1)) ? p1 : p2;
        XYZ endpoint2 = endpoint1.IsAlmostEqualTo(p1) ? p2 : p1;
        ((LocationCurve)element.Location).Curve = Line.CreateBound(endpoint1, endpoint2);

        return element;
    }

    public void CopyMepElementsToDistance(double distance, int countCopy, List<ElementWrp> mepElementModels,
        List<MepCurveWrp> mepCurveModels, XYZ direction = null)
    {
        double ft = distance.FromMillimeters();
        for (int i = 1; i <= countCopy; ++i)
            CopyingMepElementsAndConnect(ft * i, mepElementModels, mepCurveModels, direction);
    }

    /// <summary>
    /// Получает список пар незадействованных концевых коннекторов из заданных элементов MEP.
    /// </summary>
    /// <param name="mepElementsIds">Коллекция идентификаторов элементов MEP.</param>
    /// <param name="mepCurveModels">Список моделей MEP-кривых.</param>
    /// <returns>Список пар коннекторов или null, если пар недостаточно.</returns>
    private List<Tuple<ConnectorSplitModel, ConnectorSplitModel>> GetSplitConnectors(
        IEnumerable<ElementId> mepElementsIds,
        List<MepCurveWrp> mepCurveModels)
    {
        List<ConnectorSplitModel> connectorSplitModels = [];
        foreach (ElementId mepElementsId in mepElementsIds)
        {
            Element element = _doc.GetElement(mepElementsId);
            switch (element)
            {
                case FamilyInstance familyInstance:
                {
                    ConnectorManager connectorManager = familyInstance.MEPModel?.ConnectorManager;
                    if (connectorManager == null) continue;
                    foreach (Connector connector in connectorManager.Connectors.OfType<Connector>()
                                 .Where(c => c.ConnectorType == ConnectorType.End))
                    {
                        if (!connector.IsConnected && IsInMepCurve(connector, mepCurveModels, out var idMepCurve))
                            connectorSplitModels.Add(new ConnectorSplitModel(connector, idMepCurve));
                    }

                    break;
                }
                case MEPCurve mepCurve:
                {
                    foreach (Connector connector in mepCurve.ConnectorManager.Connectors.OfType<Connector>()
                                 .Where(c => c.ConnectorType == ConnectorType.End))
                    {
                        if (!connector.IsConnected && IsInMepCurve(connector, mepCurveModels, out var idMepCurve))
                            connectorSplitModels.Add(new ConnectorSplitModel(connector, idMepCurve));
                    }

                    mepCurveModels.Add(new MepCurveWrp(mepCurve));
                    break;
                }
            }
        }

        return connectorSplitModels.Count < 2 ? null : ConnectorPairFinder(connectorSplitModels);
    }

    /// <summary>
    /// Находит пары противоположных коннекторов с минимальным расстоянием внутри каждой группы MEP-кривой.
    /// </summary>
    /// <param name="connectorSplits">Список моделей разъединённых коннекторов.</param>
    /// <returns>Список пар коннекторов или null, если пары не найдены.</returns>
    private List<Tuple<ConnectorSplitModel, ConnectorSplitModel>> ConnectorPairFinder(
        List<ConnectorSplitModel> connectorSplits)
    {
        // Группируем коннекторы по идентификатору MEP-кривой, отфильтровывая группы с менее чем двумя коннекторами
        var groupedConnectors = connectorSplits
            .GroupBy(c => c.IdMepCurve)
            .Where(g => g.Count() > 1)
            .ToList();

        if (!groupedConnectors.Any())
            return null;

        var tupleList = new List<Tuple<ConnectorSplitModel, ConnectorSplitModel>>();

        foreach (var group in groupedConnectors)
        {
            double minDistance = double.MaxValue;
            Tuple<ConnectorSplitModel, ConnectorSplitModel> closestPair = null;
            var connectors = group.ToList();

            // Перебираем все уникальные пары коннекторов в группе
            for (int i = 0; i < connectors.Count; ++i)
            {
                for (int j = i + 1; j < connectors.Count; ++j)
                {
                    var connector1 = connectors[i];
                    var connector2 = connectors[j];

                    // Проверяем, являются ли коннекторы противоположными
                    if (!connector1.AreOpposite(connector2.Connector))
                        continue;

                    // Вычисляем расстояние между коннекторами
                    double distance = connector1.GetDistance(connector2);

                    // Обновляем пару с минимальным расстоянием
                    if (!(distance < minDistance)) continue;
                    minDistance = distance;
                    closestPair = new Tuple<ConnectorSplitModel, ConnectorSplitModel>(connector1, connector2);
                }
            }

            // Добавляем найденную пару в список, если она найдена
            if (closestPair != null)
                tupleList.Add(closestPair);
        }

        return tupleList;
    }

    /// <summary>
    /// Проверяет, находится ли коннектор на одной из заданных MEP-кривых.
    /// </summary>
    /// <param name="connector">Коннектор для проверки.</param>
    /// <param name="mepCurveModels">Список моделей MEP-кривых.</param>
    /// <param name="idMepCurve">Идентификатор MEP-кривой, на которой находится коннектор.</param>
    /// <returns>True, если коннектор находится на одной из кривых; иначе False.</returns>
    private bool IsInMepCurve(Connector connector, List<MepCurveWrp> mepCurveModels, out ElementId idMepCurve)
    {
        idMepCurve = null;

        var mepCurveModel = mepCurveModels.FirstOrDefault(m =>
            m.IsPointOnCurve(connector.Origin) && SameConnector(connector, m.FirstConnector));

        if (mepCurveModel == null) return false;
        idMepCurve = mepCurveModel.Id;
        return true;
    }

    /// <summary>
    /// Определяет, эквивалентны ли два коннектора по их свойствам.
    /// </summary>
    /// <param name="connector1">Первый коннектор для сравнения.</param>
    /// <param name="connector2">Второй коннектор для сравнения.</param>
    /// <returns>True, если коннекторы считаются одинаковыми; иначе False.</returns>
    private bool SameConnector(Connector connector1, Connector connector2)
    {
        if (connector1 == null || connector2 == null)
            return false;

        // Проверяем соответствие домена и формы
        if (connector1.Domain != connector2.Domain || connector1.Shape != connector2.Shape)
            return false;

        // Проверяем размеры коннекторов в зависимости от их формы
        switch (connector1.Shape)
        {
            case ConnectorProfileType.Round:
                return Math.Abs(connector1.Radius - connector2.Radius) <= 0.01;

            case ConnectorProfileType.Rectangular:
            case ConnectorProfileType.Oval:
                return Math.Abs(connector1.Width - connector2.Width) <= 0.01
                       && Math.Abs(connector1.Height - connector2.Height) <= 0.01;

            default:
                // Для других форм можно добавить дополнительную логику или вернуть false
                return false;
        }
    }
}