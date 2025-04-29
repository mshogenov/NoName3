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
            var originalTagRefs = _uidoc?.Selection.PickObject(ObjectType.Element,
                new TagSelectionFilter(), "Выберите исходную марку для копирования");
            XYZ sourceBasePoint = GetPoint("Выберите исходный опорный элемент");
            XYZ targetBasePoint = GetPoint("Выберите целевой опорный элемент");
            var originalTag = new TagData(_doc.GetElement(originalTagRefs) as IndependentTag);
            var tagsData = GetTagsData(selectedTagRefs);
            // Вычисляем вектор перемещения между опорными элементами
            XYZ translationVector = targetBasePoint - sourceBasePoint;
            var copiedTagId = CopiedTag(originalTag, translationVector);
            tagsData.RemoveAll(t => t.Id == originalTag.Id);
            var translationVector2 = GetTranslationVector(copiedTagId, originalTag);

            using Transaction trans2 = new Transaction(_doc, "Копирование марок2");
            trans2.Start();
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
            var firstTaggedElement = tagData.TaggedElements.FirstOrDefault();
            var searchPoint = firstTaggedElement.Position + translationVector;
            var newTagHead = tagData.TagHeadPosition + translationVector2;
            var nearestElement =
                new ElementModel(FindNearestElementOfCategory(_doc, searchPoint, tagData.TagCategory));
            var displacement = nearestElement.Position.Subtract(searchPoint);
            IndependentTag? newTag = null;
            if (ArePointsEqual(searchPoint, nearestElement.Position))
            {
                newTag = CreateTag(tagData, nearestElement, newTagHead, displacement, firstTaggedElement,
                    translationVector2);
            }

            Dictionary<ElementModel, ElementModel> dictionary = [];
            XYZ searchPoint2 = null;
            foreach (var taggedElement in tagData.TaggedElements)
            {
                if (taggedElement.Id.Value == firstTaggedElement.Id.Value) continue;
                searchPoint2 = taggedElement.Position + translationVector;
                var nearestElement2 =
                    new ElementModel(FindNearestElementOfCategory(_doc, searchPoint2, tagData.TagCategory));
                dictionary.Add(taggedElement, nearestElement2);
            }

            if (newTag != null)
            {
                newTag.AddReferences(dictionary.Values.Select(x => x.Reference).ToList());
                SetPositionLeaderEnd(tagData, dictionary, searchPoint2, newTag, translationVector2);
                SetPositionLeaderElbow(tagData, dictionary, searchPoint2, newTag, translationVector2);
                newTag.MergeElbows = true;
            }
        }
    }

    private IndependentTag? CreateTag(TagData tagData, ElementModel nearestElement, XYZ newTagHead, XYZ displacement,
        ElementModel firstTaggedElement, XYZ? translationVector2)
    {
        IndependentTag? newTag;
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
        newTag.TagHeadPosition = newTagHead.Add(displacement);
        newTag.LeaderEndCondition = tagData.LeaderEndCondition;
        // Если есть выноска, устанавливаем её точки
        if (!tagData.HasLeader) return newTag;
        foreach (var leader in tagData.LeadersEnd)
        {
            if (leader.TaggedElement.Id.Value == firstTaggedElement.Id.Value)
            {
                newTag.SetLeaderEnd(new Reference(nearestElement.Element),
                    leader.Position.Add(displacement).Add(translationVector2));
            }
        }

        foreach (var leader in tagData.LeadersElbow)
        {
            if (leader.TaggedElement.Id.Value == firstTaggedElement.Id.Value)
            {
                newTag.SetLeaderElbow(new Reference(nearestElement.Element),
                    leader.Position.Add(displacement).Add(translationVector2));
            }
        }

        return newTag;
    }

    private static void SetPositionLeaderElbow(TagData tagData, Dictionary<ElementModel, ElementModel> dictionary,
        XYZ searchPoint,
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
                    var displacement2 = kvp.Value.Position.Subtract(searchPoint);
                    // Передаем reference из найденной пары ключ-значение
                    newTag.SetLeaderElbow(reference, leaderElbow.Position.Add(displacement2).Add(translationVector2));
                    break;
                }
            }
        }
    }

    private static void SetPositionLeaderEnd(TagData tagData, Dictionary<ElementModel, ElementModel> dictionary,
        XYZ searchPoint, IndependentTag? newTag, XYZ? translationVector2)
    {
        foreach (var leaderEnd in tagData.LeadersEnd)
        {
            foreach (var kvp in dictionary)
            {
                ElementModel dictionaryKey = kvp.Key;
                Reference reference = kvp.Value.Reference;

                if (leaderEnd.TaggedElement.Id == dictionaryKey.Element.Id)
                {
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
    private bool ArePointsEqual(XYZ point1, XYZ point2, double tolerance = 600)
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
            IndependentTag tag = _doc.GetElement(tagRef) as IndependentTag;
            if (tag != null)
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

    // Метод для поиска ближайшего элемента указанной категории
    private Element FindNearestElementOfCategory(Document doc, XYZ searchPoint, BuiltInCategory category)
    {
        // Получаем все элементы указанной категории в активном виде
        FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
        collector.OfCategory(category);

        Element nearestElement = null;
        double minDistance = double.MaxValue;

        foreach (Element element in collector)
        {
            // Получаем все возможные точки элемента
            List<XYZ> elementPoints = GetElementPoints(element);

            foreach (XYZ point in elementPoints)
            {
                double distance = searchPoint.DistanceTo(point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestElement = element;
                }
            }
        }

        return nearestElement;
    }

    private List<XYZ> GetElementPoints(Element element)
    {
        List<XYZ> points = new List<XYZ>();

        if (element == null)
            return points;

        // Получаем геометрию элемента
        GeometryElement geomElem = element.get_Geometry(new Options());
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

        return points.Distinct().ToList();
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