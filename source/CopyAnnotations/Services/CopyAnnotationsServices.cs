using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CopyAnnotations.Filters;
using CopyAnnotations.Models;

namespace CopyAnnotations.Services;

public class CopyAnnotationsServices
{
    private readonly UIDocument? _uidoc = Context.ActiveUiDocument;
    private readonly Document? _doc = Context.ActiveDocument;

    public void CopyAnnotations()
    {
        try
        {
            List<Reference> selectedTagRefs = GetCopyTags().ToList();
            XYZ sourceBasePoint = GetPoint("Выберите исходный опорный элемент");
            XYZ targetBasePoint = GetPoint("Выберите целевой опорный элемент");
            var tagsData = GetTagsData(selectedTagRefs);
            var originalTag = tagsData.FirstOrDefault();

            // Вычисляем вектор перемещения между опорными элементами
            XYZ translationVector = targetBasePoint - sourceBasePoint;

            // Копируем тег и получаем его ID
            var copiedTagId = CopiedTag(originalTag, translationVector);
            XYZ translationVector2 = null;

            // Начинаем транзакцию
            using Transaction trans2 = new Transaction(_doc, "Копирование марок2");
            trans2.Start();

            // Проверяем, что ID не null и элемент существует в документе
            if (copiedTagId != null && _doc.GetElement(copiedTagId) != null)
            {
                // Получаем вектор трансляции, если можем
                translationVector2 = GetTranslationVector(copiedTagId, originalTag);

                // Удаляем скопированный элемент
                _doc.Delete(copiedTagId);
            }
            else
            {
                // Если не можем получить копию, просто используем основной вектор трансляции
                translationVector2 = null;
            }

            // Создаем теги с полученным вектором трансляции (или null, если его не удалось получить)
            CreateTags(tagsData, translationVector2, translationVector);

            trans2.Commit();
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // Пользователь отменил операцию
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", ex.Message);
        }
    }

    private void CreateTags(List<TagData> tagsData, XYZ? translationVector2, XYZ translationVector)
    {
        foreach (TagData tagData in tagsData)
        {
            if (tagData == null) continue;

            // Коллекция для отслеживания обработанных элементов только в этом TagData
            HashSet<ElementId> processedElementIdsInCurrentTagData = new HashSet<ElementId>();

            var firstTaggedElement = tagData.TaggedElements.FirstOrDefault();
            if (firstTaggedElement == null) continue;

            // Отмечаем первый элемент как обработанный в текущем TagData
            processedElementIdsInCurrentTagData.Add(firstTaggedElement.Id);

            IndependentTag? newTag = CreateTag(tagData, translationVector2);
            Dictionary<ElementModel, ElementModel> dictionary = [];

            foreach (var taggedElement in tagData.TaggedElements)
            {
                // Пропускаем уже обработанные элементы в этом TagData
                if (processedElementIdsInCurrentTagData.Contains(taggedElement.Id))
                    continue;

                var searchPoint2 = taggedElement.Position + translationVector2;

                // Ищем ближайший элемент, исключая уже обработанные элементы в текущем TagData
                var nearestElement = FindNearestElementOfCategory(
                    _doc,
                    searchPoint2,
                    tagData.TagCategory,
                    processedElementIdsInCurrentTagData);

                // Создаем ElementModel из найденного элемента
                var nearestElement2 = nearestElement != null
                    ? new ElementModel(nearestElement)
                    : null;

                if (nearestElement != null && ArePointsEqual(searchPoint2, nearestElement2.Position))
                {
                    dictionary.Add(taggedElement, nearestElement2);

                    // Отмечаем этот элемент как обработанный в текущем TagData
                    processedElementIdsInCurrentTagData.Add(nearestElement.Id);
                }
            }

            if (newTag != null)
            {
                newTag.AddReferences(dictionary.Values.Select(x => x.Reference).ToList());
                SetPositionLeaderElbow(tagData, dictionary, newTag, translationVector2);
                SetPositionLeaderEnd(tagData, dictionary, newTag, translationVector2);
                newTag.MergeElbows = tagData.MergeElbows;
            }
        }
    }

    private IndependentTag? CreateTag(TagData tagData, XYZ? translationVector2)
    {
        var firstTaggedElement = tagData.TaggedElements.FirstOrDefault();
        if (firstTaggedElement == null)
            return null;

        // Используем вектор трансляции или нулевой вектор, если он null
        XYZ vector = translationVector2 ?? XYZ.Zero;

        var searchPoint = firstTaggedElement.Position + vector;
        var newTagHead = tagData.TagHeadPosition + vector;

        var nearestElement = new ElementModel(FindNearestElementOfCategory(_doc, searchPoint, tagData.TagCategory));
        if (nearestElement.Element == null)
            return null;

        var displacement = nearestElement.Position.Subtract(searchPoint);
        IndependentTag? newTag = null;

        if (ArePointsEqual(searchPoint, nearestElement.Position))
        {
            try
            {
                // Создаем новую марку
                newTag = IndependentTag.Create(
                    _doc, // документ
                    tagData.TagTypeId, // тип марки
                    _doc.ActiveView.Id, // id вида
                    new Reference(nearestElement.Element), // ссылка на элемент
                    tagData.HasLeader, // наличие выноски
                    tagData.Orientation, // ориентация
                    newTagHead.Add(displacement) // позиция марки
                );
            }
            catch (Exception ex)
            {
                // Обработка возможных ошибок при создании тега
                TaskDialog.Show("Ошибка создания тега", ex.Message);
                return null;
            }
        }

        if (newTag == null)
            return null;

        newTag.TagHeadPosition = newTagHead.Add(displacement);
        newTag.LeaderEndCondition = tagData.LeaderEndCondition;

        // Если есть выноска, устанавливаем её точки
        if (!tagData.HasLeader)
            return newTag;

        foreach (var leader in tagData.LeadersEnd)
        {
            if (leader.TaggedElement.Id.Value == firstTaggedElement.Id.Value)
            {
                try
                {
                    newTag.SetLeaderEnd(
                        new Reference(nearestElement.Element),
                        leader.Position.Add(displacement).Add(vector)
                    );
                }
                catch (Exception)
                {
                    // Пропускаем ошибки при установке конца лидера
                }
            }
        }

        foreach (var leader in tagData.LeadersElbow)
        {
            if (leader.TaggedElement.Id.Value == firstTaggedElement.Id.Value)
            {
                try
                {
                    newTag.SetLeaderElbow(
                        new Reference(nearestElement.Element),
                        leader.Position.Add(displacement).Add(vector)
                    );
                }
                catch (Exception)
                {
                    // Пропускаем ошибки при установке локтя лидера
                }
            }
        }

        return newTag;
    }

    private static void SetPositionLeaderElbow(TagData tagData, Dictionary<ElementModel, ElementModel> dictionary,
        IndependentTag? newTag, XYZ? translationVector2)
    {
        foreach (var leaderElbow in tagData.LeadersElbow)
        {
            foreach (var kvp in dictionary)
            {
                ElementModel dictionaryKey = kvp.Key;
                Reference reference = kvp.Value.Reference;

                if (leaderElbow.TaggedElement.Id == dictionaryKey.Element.Id)
                {
                    var searchPoint = leaderElbow.TaggedElement.Position + translationVector2;
                    var displacement2 = kvp.Value.Position.Subtract(searchPoint);
                    // Передаем reference из найденной пары ключ-значение
                    newTag.SetLeaderElbow(reference, leaderElbow.Position.Add(displacement2).Add(translationVector2));
                    break;
                }
            }
        }
    }

    private static void SetPositionLeaderEnd(TagData tagData, Dictionary<ElementModel, ElementModel> dictionary,
        IndependentTag? newTag, XYZ? translationVector2)
    {
        foreach (var leaderEnd in tagData.LeadersEnd)
        {
            foreach (var kvp in dictionary)
            {
                ElementModel dictionaryKey = kvp.Key;
                Reference reference = kvp.Value.Reference;

                if (leaderEnd.TaggedElement.Id == dictionaryKey.Element.Id)
                {
                    var searchPoint = leaderEnd.TaggedElement.Position + translationVector2;
                    var displacement = kvp.Value.Position.Subtract(searchPoint);
                    // Передаем reference из найденной пары ключ-значение
                    newTag.SetLeaderEnd(reference, leaderEnd.Position.Add(displacement).Add(translationVector2));
                    break;
                }
            }
        }
    }

    private XYZ? GetTranslationVector(ElementId copiedTagId, TagData originalTag)
    {
        XYZ translationVector2 = null;
        if (copiedTagId != null)
        {
            var copyTag = new TagData(_doc.GetElement(copiedTagId) as IndependentTag);
            if (copyTag == null) return null;

            // Добавляем проверки на null для TagHeadPosition
            if (originalTag.TagHeadPosition == null || copyTag.TagHeadPosition == null)
            {
                // Если какая-то из позиций равна null, возвращаем null или используем альтернативу
                return null;
            }

            // Теперь можно безопасно выполнить вычитание
            translationVector2 = (originalTag.TagHeadPosition - copyTag.TagHeadPosition).Multiply(-1);
        }

        return translationVector2;
    }

    private ElementId CopiedTag(TagData originalTag, XYZ translationVector)
    {
        using Transaction trans = new Transaction(_doc, "Копирование марок");
        trans.Start();
        ElementId copiedTagId = ElementTransformUtils.CopyElement(
            _doc,
            originalTag.Id,
            translationVector
        ).First();
        trans.Commit();
        return copiedTagId;
    }

    /// <summary>
    /// Проверяет две точки на приблизительное равенство
    /// </summary>
    /// <param name="point1">Первая точка</param>
    /// <param name="point2">Вторая точка</param>
    /// <param name="tolerance">Допустимая погрешность (по умолчанию 0.001)</param>
    /// <returns>True, если точки можно считать равными</returns>
    private bool ArePointsEqual(XYZ point1, XYZ point2, double tolerance = 1000)
    {
        if (point1 == null || point2 == null)
            return false;

        // Вычисляем расстояние между точками
        double distance = point1.DistanceTo(point2).ToMillimeters();

        // Возвращаем true, если расстояние меньше или равно заданной погрешности
        return distance <= tolerance;
    }

    private List<TagData> GetTagsData(List<Reference> selectedTagRefs)
    {
        List<TagData> tagsData = [];
        foreach (Reference tagRef in selectedTagRefs)
        {
            if (_doc.GetElement(tagRef) is IndependentTag tag)
            {
                tagsData.Add(new TagData(tag));
            }
        }

        return tagsData;
    }

    private XYZ GetPoint(string status)
    {
        Reference reference = _uidoc.Selection.PickObject(ObjectType.Element,
            status);
        Element element = _doc.GetElement(reference);
        XYZ point = GetElementPosition(element);
        if (point == null)
        {
            TaskDialog.Show("Ошибка", "Не удалось определить положение элемента.");
        }

        return point;
    }

    private IList<Reference> GetCopyTags()
    {
        IList<Reference> selectedTagRefs = _uidoc.Selection.PickObjects(ObjectType.Element,
            new TagSelectionFilter(), "Выберите марки для копирования");
        return selectedTagRefs;
    }

    /// <summary>
    /// Находит ближайший элемент указанной категории, исключая уже обработанные элементы
    /// </summary>
    /// <param name="doc">Текущий документ Revit</param>
    /// <param name="searchPoint">Точка поиска</param>
    /// <param name="category">Категория искомых элементов</param>
    /// <param name="elementsToExclude">Коллекция ID элементов, которые нужно исключить из поиска</param>
    /// <param name="maxSearchDistance">Максимальное расстояние поиска (по умолчанию - 1000 мм)</param>
    /// <returns>Ближайший элемент или null, если ничего не найдено</returns>
    private Element FindNearestElementOfCategory(
        Document doc,
        XYZ searchPoint,
        BuiltInCategory category,
        ICollection<ElementId> elementsToExclude = null,
        double maxSearchDistance = 1000)
    {
        maxSearchDistance = maxSearchDistance.ToInches();
        // Оптимизация: фильтруем только видимые элементы, которые не удалены
        FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent();

        Element nearestElement = null;
        double minDistance = double.MaxValue;

        // Предварительная фильтрация по ограничивающим коробкам
        List<Element> potentialElements = new List<Element>();

        foreach (Element element in collector)
        {
            // Пропускаем элементы из списка исключений
            if (elementsToExclude != null && elementsToExclude.Contains(element.Id))
                continue;

            BoundingBoxXYZ bb = element.get_BoundingBox(null);
            if (bb != null)
            {
                // Расширяем ограничивающую коробку на величину maxSearchDistance
                XYZ expandedMin = new XYZ(
                    bb.Min.X - maxSearchDistance,
                    bb.Min.Y - maxSearchDistance,
                    bb.Min.Z - maxSearchDistance);

                XYZ expandedMax = new XYZ(
                    bb.Max.X + maxSearchDistance,
                    bb.Max.Y + maxSearchDistance,
                    bb.Max.Z + maxSearchDistance);

                // Проверяем, находится ли точка поиска внутри расширенной коробки
                if (IsPointInBox(searchPoint, expandedMin, expandedMax))
                {
                    potentialElements.Add(element);
                }
            }
            else
            {
                // Если нет ограничивающей коробки, добавляем элемент для дальнейшей проверки
                potentialElements.Add(element);
            }
        }

        // Обрабатываем только потенциально подходящие элементы
        foreach (Element element in potentialElements)
        {
            double distance = GetMinDistanceToElement(element, searchPoint);

            // Проверяем, что расстояние в пределах заданного диапазона
            if (distance < minDistance && distance <= maxSearchDistance)
            {
                minDistance = distance;
                nearestElement = element;
            }
        }

        return nearestElement;
    }

    /// <summary>
    /// Находит все элементы указанной категории в пределах заданного расстояния
    /// </summary>
    /// <returns>Список элементов с их расстояниями, отсортированный по возрастанию расстояния</returns>
    private List<(Element Element, double Distance)> FindElementsInRange(
        Document doc,
        XYZ searchPoint,
        BuiltInCategory category,
        double maxSearchDistance)
    {
        FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent();

        List<(Element Element, double Distance)> result = new List<(Element, double)>();

        foreach (Element element in collector)
        {
            double distance = GetMinDistanceToElement(element, searchPoint);

            if (distance <= maxSearchDistance)
            {
                result.Add((element, distance));
            }
        }

        // Сортируем результаты по расстоянию (от ближайшего к дальнему)
        return result.OrderBy(item => item.Distance).ToList();
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри прямоугольной области
    /// </summary>
    private bool IsPointInBox(XYZ point, XYZ min, XYZ max)
    {
        return
            point.X >= min.X && point.X <= max.X &&
            point.Y >= min.Y && point.Y <= max.Y &&
            point.Z >= min.Z && point.Z <= max.Z;
    }

// Новый метод для расчета минимального расстояния до элемента
    private double GetMinDistanceToElement(Element element, XYZ searchPoint)
    {
        List<XYZ> elementPoints = GetElementPoints(element);

        if (elementPoints.Count == 0)
            return double.MaxValue;

        return elementPoints.Min(point => searchPoint.DistanceTo(point));
    }


// Новый метод для рекурсивной обработки геометрии
    private void ProcessGeometryElement(GeometryElement geomElem, List<XYZ> points)
    {
        foreach (GeometryObject geomObj in geomElem)
        {
            if (geomObj is Solid solid && solid.Volume > 0)
            {
                // Добавляем точки только для непустых солидов
                foreach (Edge edge in solid.Edges)
                {
                    points.Add(edge.AsCurve().GetEndPoint(0));
                    points.Add(edge.AsCurve().GetEndPoint(1));
                }

                // Добавляем точки из центров граней для более точного поиска
                foreach (Face face in solid.Faces)
                {
                    // Находим "центр тяжести" грани
                    BoundingBoxUV boundingBox = face.GetBoundingBox();
                    UV centerUV = new UV(
                        (boundingBox.Min.U + boundingBox.Max.U) / 2,
                        (boundingBox.Min.V + boundingBox.Max.V) / 2
                    );
                    XYZ centerPoint = face.Evaluate(centerUV);
                    points.Add(centerPoint);
                }
            }
            else if (geomObj is Curve curve)
            {
                // Обрабатываем все типы кривых, не только линии
                points.Add(curve.GetEndPoint(0));
                points.Add(curve.GetEndPoint(1));

                // Добавляем середину кривой
                points.Add(curve.Evaluate(0.5, true));
            }
            else if (geomObj is Point point)
            {
                points.Add(point.Coord);
            }
            else if (geomObj is GeometryInstance instance)
            {
                // Обрабатываем вложенную геометрию
                ProcessGeometryElement(instance.GetInstanceGeometry(), points);
            }
        }
    }

    private List<XYZ> GetElementPoints(Element element)
    {
        List<XYZ> points = new List<XYZ>();

        if (element == null)
            return points;
        try
        {
            Options options = new Options
            {
                ComputeReferences = false,
                DetailLevel = ViewDetailLevel.Medium
            };

            GeometryElement geomElem = element.get_Geometry(options);
            if (geomElem != null)
            {
                // Рекурсивно обрабатываем геометрию, включая вложенные элементы
                ProcessGeometryElement(geomElem, points);
            }

            if (geomElem != null)
            {
                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is Solid solid)
                    {
                        // Добавляем все вершины солида
                        foreach (Edge edge in solid.Edges)
                        {
                            points.Add(edge.AsCurve().GetEndPoint(0));
                            points.Add(edge.AsCurve().GetEndPoint(1));
                        }
                    }
                    else if (geomObj is Line line)
                    {
                        points.Add(line.GetEndPoint(0));
                        points.Add(line.GetEndPoint(1));
                    }
                    else if (geomObj is Point point)
                    {
                        points.Add(point.Coord);
                    }
                }
            }

            // Добавляем точку расположения
            Location location = element.Location;
            if (location is LocationPoint locationPoint)
            {
                points.Add(locationPoint.Point);
            }
            else if (location is LocationCurve locationCurve)
            {
                points.Add(locationCurve.Curve.GetEndPoint(0));
                points.Add(locationCurve.Curve.GetEndPoint(1));
            }

            // Добавляем точки ограничивающего бокса
            BoundingBoxXYZ bb = element.get_BoundingBox(null);
            if (bb != null)
            {
                points.Add(bb.Min);
                points.Add(bb.Max);
                points.Add((bb.Min + bb.Max) * 0.5); // центр
            }

            return points.Count <= 1 ? points : new HashSet<XYZ>(points, new XYZEqualityComparer()).ToList();
        }
        catch (Exception ex)
        {
            // Добавление обработки ошибок
            TaskDialog.Show("Ошибка", $"Не удалось получить точки элемента: {ex.Message}");
            return points;
        }
    }

    // Метод для получения позиции элемента
    private XYZ GetElementPosition(Element element)
    {
        if (element == null)
            return null;

        // Пробуем получить точку расположения
        Location location = element.Location;
        if (location is LocationPoint locationPoint)
        {
            return locationPoint.Point;
        }

        if (location is LocationCurve locationCurve)
        {
            return (locationCurve.Curve as Line)?.Origin;
        }

        // Если точки расположения нет, используем центр ограничивающего бокса
        BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
        if (boundingBox != null)
        {
            return (boundingBox.Min + boundingBox.Max) * 0.5;
        }

        return null;
    }
}