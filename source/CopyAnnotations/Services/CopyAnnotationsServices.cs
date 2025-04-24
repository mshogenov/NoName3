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
            XYZ sourceBasePoint = GetPoint("Выберите исходный опорный элемент");
            XYZ targetBasePoint = GetPoint("Выберите целевой опорный элемент");

            // Вычисляем вектор перемещения между опорными элементами
            XYZ translationVector = targetBasePoint - sourceBasePoint;

            using (Transaction trans = new Transaction(_doc, "Копирование независимых марок"))
            {
                trans.Start();

                foreach (Reference tagRef in selectedTagRefs)
                {
                    // Получаем оригинальную марку
                    Element element = _doc.GetElement(tagRef);
                    IndependentTag originalTag = element as IndependentTag;

                    if (originalTag != null)
                    {
                        // Прямое копирование через ElementTransformUtils
                        ICollection<ElementId> copiedIds = ElementTransformUtils.CopyElement(
                            _doc,
                            originalTag.Id,
                            translationVector
                        );

                        if (copiedIds.Count == 0)
                        {
                            TaskDialog.Show("Предупреждение", "Не удалось скопировать марку");
                        }
                    }
                }

                trans.Commit();
            }
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
    private Element FindNearestElementOfCategory(Document doc, XYZ searchPoint, Category category)
    {
        // Получаем все элементы указанной категории в активном виде
        FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
        collector.OfCategoryId(category.Id);

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
            return locationCurve.Curve.Evaluate(0.5, true); // Центральная точка кривой
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