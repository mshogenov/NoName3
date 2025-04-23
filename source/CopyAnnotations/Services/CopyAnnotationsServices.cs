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
            List<TagData> tagsData = GetTagsData(selectedTagRefs, sourceBasePoint);
            // 3. Создаем новые марки в указанном месте
            using (Transaction trans = new Transaction(_doc, "Копирование независимых марок"))
            {
                trans.Start();

                foreach (TagData tagData in tagsData)
                {
                    // Вычисляем новую позицию марки относительно целевой базовой точки
                    XYZ newPosition = targetBasePoint + tagData.RelativePosition;

                    // Находим ближайший элемент соответствующей категории
                    if (tagData.TaggedElementCategory != null)
                    {
                        // Определяем точку для поиска ближайшего элемента
                        XYZ searchPoint;
                        if (tagData.Leader && tagData.RelativeLeaderEnd != null)
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
                                tagData.Leader, // Нужна ли выноска
                                tagData.Orientation, // Ориентация марки
                                newPosition // Позиция марки
                            );


                            // Настраиваем положение лидера, если он есть
                            if (newTag != null && tagData.Leader)
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
                    }
                }

                trans.Commit();
            }

            return;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            return;
        }
    }

    private List<TagData> GetTagsData(List<Reference> selectedTagRefs, XYZ sourceBasePoint)
    {
        List<TagData> tagsData = [];
        foreach (Reference tagRef in selectedTagRefs)
        {
            IndependentTag tag = _doc.GetElement(tagRef) as IndependentTag;
            if (tag != null)
            {
                // Получаем элемент, к которому привязана марка
                Element taggedElement = null;
                Reference taggedElementRef = null;

                ICollection<LinkElementId> taggedElementIds = tag.GetTaggedElementIds();
                if (taggedElementIds is { Count: > 0 })
                {
                    taggedElement = _doc.GetElement(taggedElementIds.First().HostElementId);
                    if (taggedElement != null)
                    {
                        taggedElementRef = new Reference(taggedElement);
                    }
                }
                // Сохраняем категорию элемента, к которому привязана марка
                ElementId categoryId = taggedElement.Category?.Id;

                // Сохраняем относительное положение от базовой точки
                XYZ relativePosition = tag.TagHeadPosition - sourceBasePoint;

                if (tag.HasLeader && taggedElementRef != null)
                {
                    try
                    {
                        // Получаем конец выноски и локоть выноски
                        XYZ leaderEnd = tag.GetLeaderEnd(taggedElementRef);
                        XYZ leaderElbow = tag.GetLeaderElbow(taggedElementRef);

                        // Сохраняем относительные позиции от базовой точки
                        XYZ relativeLeaderEnd = leaderEnd - sourceBasePoint;
                        XYZ relativeLeaderElbow = leaderElbow - sourceBasePoint;
                        LeaderEndCondition leaderEndCondition = tag.LeaderEndCondition;
                        // И вектор направления выноски относительно позиции марки (для сохранения угла)
                        XYZ leaderVector = leaderEnd - tag.TagHeadPosition;

                        TagData tagData = new TagData(tag)
                        {
                            TagType = tag.GetTypeId(),
                            TaggedElementCategory = categoryId,
                            RelativePosition = relativePosition,
                            RelativeLeaderEnd = relativeLeaderEnd, // Относительно базовой точки
                            RelativeLeaderElbow = relativeLeaderElbow, // Относительно базовой точки
                            LeaderVector = leaderVector, // Относительно марки 
                            Orientation = tag.TagOrientation,
                            Leader = tag.HasLeader,
                            LeaderEndCondition = leaderEndCondition
                        };

                        tagsData.Add(tagData);
                    }
                    catch (Exception)
                    {
                        // Если получить данные о лидере не удалось, оставляем null
                    }
                }
                else
                {
                    // Для марок без выноски
                    TagData tagData = new TagData(tag)
                    {
                        TagType = tag.GetTypeId(),
                        TaggedElementCategory = categoryId,
                        RelativePosition = relativePosition,
                        Orientation = tag.TagOrientation,
                        Leader = tag.HasLeader
                    };

                    tagsData.Add(tagData);
                }
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
    private Element FindNearestElementOfCategory(Document doc, XYZ searchPoint, ElementId categoryId)
    {
        // Получаем все элементы указанной категории в активном виде
        FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
        collector.OfCategoryId(categoryId);

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