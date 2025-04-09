using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External;
using RevitAddIn1.Filters;
using RevitAddIn1.Models;

namespace RevitAddIn1.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class CopyAnnotationsCommand : ExternalCommand
{
    public override void Execute()
    {
        UIDocument uidoc = Context.ActiveUiDocument;
        Document doc = uidoc.Document;

        try
        {
            
            IList<Reference> selectedTagRefs = uidoc.Selection.PickObjects(ObjectType.Element,
                new TagSelectionFilter(), "Выберите марки для копирования");

            if (selectedTagRefs.Count == 0)
            {
                return;
            }

           
            XYZ sourceBasePoint = uidoc.Selection.PickPoint("Укажите базовую точку копирования");

          
            XYZ targetBasePoint = uidoc.Selection.PickPoint("Укажите точку назначения для копирования марок");
            // Сохраняем выбранные марки и их данные
            // Сохраняем выбранные марки и их данные
            List<TagData> tagsData = new List<TagData>();


            foreach (Reference tagRef in selectedTagRefs)
            {
                IndependentTag tag = doc.GetElement(tagRef) as IndependentTag;
                if (tag != null)
                {
                    // Получаем элемент, к которому привязана марка
                    Element taggedElement = null;
                    ElementId taggedElementId = null;
                    Reference taggedElementRef = null;

                    // Для Revit 2023 и выше
                    ICollection<LinkElementId> taggedElementIds = tag.GetTaggedElementIds();
                    if (taggedElementIds != null && taggedElementIds.Count > 0)
                    {
                        taggedElementId = taggedElementIds.First().HostElementId;
                        taggedElement = doc.GetElement(taggedElementId);

                        if (taggedElement != null)
                        {
                            taggedElementRef = new Reference(taggedElement);
                        }
                    }

                    // Сохраняем категорию элемента, к которому привязана марка
                    ElementId categoryId = (taggedElement != null) ? taggedElement.Category.Id : null;

                    // Сохраняем относительное положение от базовой точки
                    XYZ relativePosition = tag.TagHeadPosition - sourceBasePoint;

                    // Получаем данные о выноске
                    XYZ leaderVector = null;


                    if (tag.HasLeader && taggedElementRef != null)
                    {
                        try
                        {
                            // Получаем конец выноски
                            XYZ leaderEnd = tag.GetLeaderEnd(taggedElementRef);
                            // Сохраняем и относительную позицию конца выноски от базовой точки
                            XYZ relativeLeaderEnd = leaderEnd - sourceBasePoint;

                            // И вектор направления выноски относительно позиции марки (для сохранения угла)
                            leaderVector = leaderEnd - tag.TagHeadPosition;
                            TagData tagData = new TagData
                            {
                                TagType = tag.GetTypeId(),
                                TaggedElementCategory = categoryId,
                                RelativePosition = relativePosition,
                                RelativeLeaderEnd = relativeLeaderEnd, // Относительно базовой точки
                                LeaderVector = leaderVector, // Относительно марки 
                                Orientation = tag.TagOrientation,
                                Leader = tag.HasLeader
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
                        TagData tagData = new TagData
                        {
                            TagType = tag.GetTypeId(),
                            TaggedElementCategory = categoryId,
                            RelativePosition = relativePosition,
                            Orientation = tag.TagOrientation,
                            Leader = false
                        };

                        tagsData.Add(tagData);
                    }
                }
            }

            // 3. Создаем новые марки в указанном месте
            using (Transaction trans = new Transaction(doc, "Копирование независимых марок"))
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
                            FindNearestElementOfCategory(doc, searchPoint, tagData.TaggedElementCategory);

                        if (nearestElement != null)
                        {
                            // Создаем новую марку для найденного элемента
                            IndependentTag newTag = null;

                            // Для Revit 2023+
                            Reference elementRef = new Reference(nearestElement);
                            newTag = IndependentTag.Create(
                                doc,
                                tagData.TagType, // symId - ElementId типа марки
                                doc.ActiveView.Id, // ownerDBViewId - ElementId текущего вида
                                elementRef, // referenceToTag - ссылка на элемент для маркировки
                                tagData.Leader, // addLeader - нужна ли выноска
                                tagData.Orientation, // tagOrientation - ориентация марки
                                newPosition // pnt - позиция марки
                            );

                            // Настраиваем положение лидера, если он есть
                            if (newTag != null && tagData.Leader)
                            {
                                // В Revit 2023+ метод SetLeaderEnd требует ссылку на элемент и точку конца
                                if (tagData.LeaderVector != null)
                                {
                                    try
                                    {
                                        // Вычисляем точку конца выноски, сохраняя тот же вектор
                                        // направления выноски, что был у оригинальной марки
                                        XYZ leaderEndPoint = newTag.TagHeadPosition + tagData.LeaderVector;

                                        // Устанавливаем конечную точку лидера
                                        newTag.SetLeaderEnd(elementRef, leaderEndPoint);
                                    }
                                    catch (Exception)
                                    {
                                        // Игнорируем ошибки при настройке лидера
                                    }
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