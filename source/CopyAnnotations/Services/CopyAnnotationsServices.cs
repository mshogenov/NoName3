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
            // Сохраняем выбранные марки и их данные
            List<TagData> tagsData = GetTagsData(selectedTagRefs);
            // 3. Создаем новые марки в указанном месте
            using Transaction trans = new Transaction(_doc, "Копирование независимых марок");
            trans.Start();
            foreach (TagData tagData in tagsData)
            {
                tagData.GetRelativePositions(sourceBasePoint);
                // Вычисляем новую позицию марки относительно целевой базовой точки
                XYZ newPosition = targetBasePoint + tagData.RelativePosition;

                // Находим ближайший элемент соответствующей категории
                if (tagData.TaggedElementCategory != null)
                {
                    // Определяем точку для поиска ближайшего элемента
                    XYZ searchPoint;
                    if (tagData is { HasLeader: true})
                    {
                        // Если есть выноска, ищем элемент в точке конца выноски
                        searchPoint = targetBasePoint + tagData.RelativeLeaderEnd;
                    }
                    else
                    {
                        // Иначе ищем рядом с позицией марки
                        searchPoint = newPosition;
                    }
                    Element nearestElement =
                        FindNearestElementOfCategory(_doc, searchPoint, tagData.TaggedElementCategory);
                    if (nearestElement != null)
                    {
                        // Создаем новую марку для найденного элемента
                        Reference elementRef = new Reference(nearestElement);

                        IndependentTag newTag = IndependentTag.Create(
                            _doc,
                            tagData.TagType, // Тип марки
                            _doc.ActiveView.Id, // ID текущего вида
                            elementRef, // Ссылка на элемент для маркировки
                            tagData.HasLeader, // Нужна ли выноска
                            tagData.Orientation, // Ориентация марки
                            newPosition // Позиция марки
                        );
                        if (newTag != null && tagData.HasLeader)
                        {
                            // Настраиваем положение лидера, если он есть
                            AdjustPositionLeader(newTag, tagData, targetBasePoint, elementRef); 
                        }
                     
                    }
                }
            }

            trans.Commit();
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
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