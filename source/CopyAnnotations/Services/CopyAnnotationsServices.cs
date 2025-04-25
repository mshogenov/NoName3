using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CopyAnnotations.Filters;
using CopyAnnotations.Models;

namespace CopyAnnotations.Services;

public class CopyAnnotationsServices
{
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private readonly Document _doc = Context.ActiveDocument;

    public void CopyAnnotations()
    {
        try
        {
            List<Reference> selectedTagRefs = GetCopyTags().ToList();
            var originalTagRefs = _uidoc.Selection.PickObject(ObjectType.Element,
                new TagSelectionFilter(), "Выберите исходную марку для копирования");
            XYZ sourceBasePoint = GetPoint("Выберите исходный опорный элемент");
            XYZ targetBasePoint = GetPoint("Выберите целевой опорный элемент");
            var originalTag = new TagData(_doc.GetElement(originalTagRefs) as IndependentTag);
            var tagsData = GetTagsData(selectedTagRefs);
            // Вычисляем вектор перемещения между опорными элементами
            XYZ translationVector = targetBasePoint - sourceBasePoint;
            using Transaction trans = new Transaction(_doc, "Копирование марок");
            trans.Start();
            ElementId copiedTagId = ElementTransformUtils.CopyElement(
                _doc,
                originalTag.Id,
                translationVector
            ).First();
            trans.Commit();
            XYZ translationVector2 = null;
            if (copiedTagId != null)
            {
                var copyTag = new TagData(_doc.GetElement(copiedTagId) as IndependentTag);
                translationVector2 = (originalTag.TagHeadPosition - copyTag.TagHeadPosition).Multiply(-1);
            }

            using Transaction trans2 = new Transaction(_doc, "Копирование марок2");
            trans2.Start();
            tagsData.RemoveAll(t => t.Id == originalTag.Id);
            foreach (TagData tagData in tagsData)
            {
                if (tagData != null)
                {
                    var newLeaderEnd = tagData.LeaderEnd + translationVector2;
                    var newTagHead = tagData.TagHeadPosition + translationVector2;
                    var newLeaderElbow = tagData.LeaderElbow + translationVector2;
                    var nearestElement = FindNearestElementOfCategory(_doc, newLeaderEnd, tagData.TagCategory);

                    // Создаем новую марку
                    IndependentTag newTag = IndependentTag.Create(
                        _doc, // документ
                        tagData.TagTypeId, // тип марки
                        _doc.ActiveView.Id, // id вида
                        new Reference(nearestElement), // ссылка на элемент
                        tagData.HasLeader, // наличие выноски
                        tagData.Orientation, // ориентация
                        newTagHead // позиция марки
                    );

                    // Если есть выноска, устанавливаем её точки
                    if (tagData.HasLeader)
                    {
                        newTag.TagHeadPosition = newTagHead;
                        newTag.LeaderEndCondition = tagData.LeaderEndCondition;
                        newTag.SetLeaderEnd(new Reference(nearestElement), newLeaderEnd);
                        if (tagData.LeaderElbow != null)
                        {
                            newTag.SetLeaderElbow(new Reference(nearestElement), newLeaderElbow);
                        }
                    }
                }
            }

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

    // Метод для поиска ближайшего элемента заданной категории
    private Element FindNearestElement(XYZ position, BuiltInCategory category)
    {
        // Определяем радиус поиска
        double searchRadius = 5.0; // в футах, можно настроить

        Element nearestElement = null;
        double minDistance = double.MaxValue;

        // Создаем Outline для BoundingBoxIntersectsFilter
        XYZ minPoint = position - new XYZ(searchRadius, searchRadius, searchRadius);
        XYZ maxPoint = position + new XYZ(searchRadius, searchRadius, searchRadius);
        Outline searchBox = new Outline(minPoint, maxPoint);

        // Фильтр по категории
        ElementCategoryFilter categoryFilter = new ElementCategoryFilter(category);

        // Получаем все элементы в заданном радиусе с нужной категорией
        FilteredElementCollector collector = new FilteredElementCollector(_doc, _doc.ActiveView.Id)
            .WherePasses(categoryFilter)
            .WherePasses(new BoundingBoxIntersectsFilter(searchBox));

        foreach (Element element in collector)
        {
            // Используем Location элемента для определения его положения
            Location location = element.Location;
            XYZ elementPosition;

            if (location is LocationPoint locationPoint)
            {
                elementPosition = locationPoint.Point;
            }
            else if (location is LocationCurve locationCurve)
            {
                // Для линейных элементов берем середину кривой
                elementPosition = (locationCurve.Curve.GetEndPoint(0) + locationCurve.Curve.GetEndPoint(1)) / 2;
            }
            else
            {
                continue; // Пропускаем, если невозможно определить положение
            }

            double distance = position.DistanceTo(elementPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestElement = element;
            }
        }

        return nearestElement;
    }

    private XYZ GetCorrectedPosition(Element element, XYZ proposedPosition)
    {
        // Здесь реализуйте логику корректировки положения марки
        // в зависимости от геометрии элемента

        // Пример: проецирование точки на ближайшую грань элемента
        Options options = new Options();
        GeometryElement geomElem = element.get_Geometry(options);
        XYZ closestPoint = proposedPosition;
        double minDistance = double.MaxValue;

        foreach (GeometryObject geomObj in geomElem)
        {
            if (geomObj is Solid solid)
            {
                foreach (Face face in solid.Faces)
                {
                    XYZ pointOnFace = face.Project(proposedPosition).XYZPoint;
                    double distance = proposedPosition.DistanceTo(pointOnFace);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = pointOnFace;
                    }
                }
            }
        }

        return closestPoint;
    }

    private bool IsVectorAlmostEqual(XYZ vector1, XYZ vector2, double tolerance = 0.001)
    {
        if (vector1 == null || vector2 == null)
            return false;

        // Сравниваем каждую координату
        return Math.Abs(vector1.X - vector2.X) <= tolerance &&
               Math.Abs(vector1.Y - vector2.Y) <= tolerance &&
               Math.Abs(vector1.Z - vector2.Z) <= tolerance;
    }

    /// <summary>
    /// Проверяет две точки на приблизительное равенство
    /// </summary>
    /// <param name="point1">Первая точка</param>
    /// <param name="point2">Вторая точка</param>
    /// <param name="tolerance">Допустимая погрешность (по умолчанию 0.001)</param>
    /// <returns>True, если точки можно считать равными</returns>
    private bool ArePointsEqual(XYZ point1, XYZ point2, double tolerance = 0.001)
    {
        if (point1 == null || point2 == null)
            return false;

        // Вычисляем расстояние между точками
        double distance = point1.DistanceTo(point2);

        // Возвращаем true, если расстояние меньше или равно заданной погрешности
        return distance <= tolerance;
    }

    private static void AdjustPositionLeader(IndependentTag newTag, TagData tagData, XYZ targetBasePoint,
        Reference elementRef)
    {
        if (newTag != null && tagData.HasLeader)
        {
            try
            {
                // Устанавливаем тип условия конца лидера
                if (tagData.LeaderEndCondition != null)
                {
                    newTag.LeaderEndCondition = tagData.LeaderEndCondition;
                }

                // Вычисляем новые координаты конца выноски относительно целевой точки
                if (tagData.RelativeLeaderEnd != null)
                {
                    XYZ newLeaderEnd = targetBasePoint + tagData.RelativeLeaderEnd;
                    newTag.SetLeaderEnd(elementRef, newLeaderEnd);
                }
                else if (tagData.LeaderVector != null)
                {
                    // Альтернативный способ: используя вектор направления
                    XYZ leaderEndPoint = newTag.TagHeadPosition + tagData.LeaderVector;
                    newTag.SetLeaderEnd(elementRef, leaderEndPoint);
                }

                // Устанавливаем локоть лидера относительно целевой точки
                if (tagData.RelativeLeaderElbow != null)
                {
                    XYZ newLeaderElbow = targetBasePoint + tagData.RelativeLeaderElbow;
                    newTag.SetLeaderElbow(elementRef, newLeaderElbow);
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки при настройке лидера
            }
        }
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
            // Получаем точку для измерения расстояния
            // Используем либо LocationPoint, либо BoundingBox
            XYZ elementPoint = GetElementPosition(element);

            if (elementPoint != null)
            {
                double distance = searchPoint.DistanceTo(elementPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestElement = element;
                }
            }
        }

        return nearestElement;
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
        else if (location is LocationCurve locationCurve)
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