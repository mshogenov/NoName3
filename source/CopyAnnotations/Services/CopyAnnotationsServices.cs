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
        List<Reference> selectedTagRefs = [];
        XYZ sourceBasePoint = null;
        XYZ targetBasePoint = null;
        try
        {
            selectedTagRefs = GetCopyTags().ToList();
            sourceBasePoint = GetPoint("Выберите исходный опорный элемент");
            targetBasePoint = GetPoint("Выберите целевой опорный элемент");
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return;
        }

        List<TagModel> tagModels = [];
        List<TextNoteModel> textNoteModels = [];
        List<AnnotationSymbol> annotationSymbols = [];
        List<DimensionModel> dimensionModels = [];
        foreach (Reference tagRef in selectedTagRefs)
        {
            if (_doc?.GetElement(tagRef) is IndependentTag tag)
            {
                tagModels.Add(new TagModel(tag));
            }

            if (_doc?.GetElement(tagRef) is Dimension dimension)
            {
                dimensionModels.Add(new DimensionModel(dimension));
            }

            if (_doc?.GetElement(tagRef) is TextNote textNote)
            {
                textNoteModels.Add(new TextNoteModel(textNote));
            }

            if (_doc?.GetElement(tagRef) is AnnotationSymbol annotationSymbol)
            {
                annotationSymbols.Add(annotationSymbol);
            }
        }

        // Вычисляем вектор перемещения между опорными элементами
        XYZ translationVector = targetBasePoint - sourceBasePoint;
        XYZ vector = null;
        using TransactionGroup tg = new TransactionGroup(_doc, "Копирование аннотаций");
        tg.Start();
        if (tagModels.Any())
        {
            var originalTag = tagModels.FirstOrDefault();
            if (originalTag == null) return;
            using Transaction trans = new Transaction(_doc, "Копирование первой аннотации");
            trans.Start();
            ElementId copiedTagId = CopiedTag(originalTag.IndependentTag, translationVector);
            if (copiedTagId == null)
            {
                trans.RollBack();
                tg.RollBack();
                TaskDialog.Show("Ошибка", "Не удалось скопировать аннотации.");
                return;
            }

            trans.Commit();
            if (tagModels.Count > 1)
            {
                using Transaction trans2 = new Transaction(_doc, "Копирование марок");
                trans2.Start();
                if (copiedTagId != null && _doc?.GetElement(copiedTagId) != null)
                {
                    // Получаем вектор трансляции, если можем
                    vector = GetTranslationVectorTag(copiedTagId, originalTag);
                    // Удаляем скопированный элемент
                    _doc.Delete(copiedTagId);
                    CreateTags(tagModels, vector);
                }
                else
                {
                    trans2.RollBack();
                    tg.RollBack();
                    TaskDialog.Show("Ошибка", "Не удалось скопировать аннотации.");
                    return;
                }

                trans2.Commit();
            }
        }

        if (dimensionModels.Any())
        {
            if (vector == null)
            {
                var originalDimension = dimensionModels.FirstOrDefault();
                if (originalDimension == null) return;
                using Transaction trans = new Transaction(_doc, "Копирование первой аннотации");
                trans.Start();
                ElementId copiedTagId = CopiedTag(originalDimension.Dimension, translationVector);
                if (copiedTagId == null)
                {
                    trans.RollBack();
                    tg.RollBack();
                    TaskDialog.Show("Ошибка", "Не удалось скопировать аннотации.");
                    return;
                }

                trans.Commit();
                if (dimensionModels.Count > 1)
                {
                    using Transaction trans2 = new Transaction(_doc, "Копирование марок");
                    trans2.Start();
                    if (copiedTagId != null && _doc?.GetElement(copiedTagId) != null)
                    {
                        // Получаем вектор трансляции, если можем
                        vector = GetTranslationVectorDimension(copiedTagId, originalDimension);
                        // Удаляем скопированный элемент
                        _doc.Delete(copiedTagId);
                        CreateDimensions(dimensionModels, vector);
                    }
                    else
                    {
                        trans2.RollBack();
                        tg.RollBack();
                        TaskDialog.Show("Ошибка", "Не удалось скопировать аннотации.");
                        return;
                    }

                    trans2.Commit();
                }
            }
            else
            {
                CreateDimensions(dimensionModels, vector);
            }
        }

        if (textNoteModels.Any())
        {
            using Transaction trans = new Transaction(_doc, "Копирование текстовых примечаний");
            trans.Start();
            foreach (var textNote in textNoteModels)
            {
                ElementId copiedTagId = CopiedTag(textNote.TextNote, translationVector);
            }

            trans.Commit();
        }

        if (annotationSymbols.Any())
        {
            using Transaction trans = new Transaction(_doc, "Копирование обозначений");
            trans.Start();
            foreach (var annotationSymbol in annotationSymbols)
            {
                ElementId copiedTagId = CopiedTag(annotationSymbol, translationVector);
            }

            trans.Commit();
        }

        tg.Commit();
    }

    private void CreateTags(List<TagModel> tagsData, XYZ? translationVector)
    {
        if (!tagsData.Any() || translationVector == null) return;

        foreach (TagModel tagData in tagsData)
        {
            HashSet<ElementId?> processedElementIdsInCurrentTagData = [];
            var firstTaggedElement = tagData.TaggedElements.FirstOrDefault();
            if (firstTaggedElement == null) continue;

            // Отмечаем первый элемент как обработанный в текущем TagData
            processedElementIdsInCurrentTagData.Add(firstTaggedElement.Id);
            IndependentTag? newTag = CreateTag(tagData, translationVector);

            if (newTag == null) continue;

            Dictionary<ElementModel, ElementModel> dictionary = [];
            foreach (var taggedElement in tagData.TaggedElements)
            {
                // Пропускаем уже обработанные элементы в этом TagData
                if (processedElementIdsInCurrentTagData.Contains(taggedElement.Id))
                    continue;

                var searchPoint = taggedElement.Position + translationVector;

                // Ищем ближайший элемент, исключая уже обработанные элементы в текущем TagData
                ElementModel nearestElement = new ElementModel(FindNearestElementOfCategory(
                    _doc,
                    searchPoint,
                    tagData.TagCategory,
                    processedElementIdsInCurrentTagData));

                if (nearestElement.Element == null || !ArePointsEqual(searchPoint, nearestElement.Position)) continue;

                // Проверяем, не имеет ли элемент уже тег
                if (!IsElementAlreadyTagged(nearestElement.Element, _doc.ActiveView))
                {
                    dictionary.Add(taggedElement, nearestElement);
                    // Отмечаем этот элемент как обработанный в текущем TagData
                    processedElementIdsInCurrentTagData.Add(nearestElement.Id);
                }
            }

            // Добавляем ссылки только если есть валидные элементы для добавления
            if (dictionary.Count > 0)
            {
                try
                {
                    var referencesToAdd = dictionary.Values
                        .Select(x => x.Reference)
                        .ToList();

                    if (referencesToAdd.Count > 0)
                    {
                        newTag.AddReferences(referencesToAdd);
                        SetPositionLeaderElbow(tagData, dictionary, newTag, translationVector);
                        SetPositionLeaderEnd(tagData, dictionary, newTag, translationVector);
                        newTag.MergeElbows = tagData.MergeElbows;
                    }
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                {
                    // Обрабатываем ошибку и продолжаем работу
                    TaskDialog.Show("Предупреждение", $"Не удалось добавить ссылки к тегу: {ex.Message}");
                }
            }
        }
    }

// Проверка, имеет ли элемент уже тег в текущем виде
    private bool IsElementAlreadyTagged(Element element, View view)
    {
        if (element == null) return true;

        // Получаем все теги в текущем виде
        FilteredElementCollector collector = new FilteredElementCollector(_doc, view.Id)
            .OfClass(typeof(IndependentTag));

        foreach (IndependentTag tag in collector)
        {
            // Проверяем, ссылается ли тег на наш элемент
            if (tag.GetTaggedReferences().Any(r => r.ElementId == element.Id))
            {
                return true;
            }
        }

        return false;
    }


    private void CreateDimensions(List<DimensionModel> dimensionModels, XYZ? translationVector)
    {
        if (!dimensionModels.Any() || translationVector == null) return;
        foreach (DimensionModel dimensionModel in dimensionModels)
        {
            HashSet<ElementId?> processedElementIdsInCurrentTagData = [];
            var firstTaggedElement = dimensionModel.References.FirstOrDefault().TaggedElement;
            if (firstTaggedElement == null) continue;
            // Отмечаем первый элемент как обработанный в текущем TagData
            processedElementIdsInCurrentTagData.Add(firstTaggedElement.Id);
            Dimension? newTag = CreateDimension(dimensionModel, translationVector);
        }
    }

    private IndependentTag? CreateTag(TagModel tagModel, XYZ? translationVector2)
    {
        var firstTaggedElement = tagModel.TaggedElements.FirstOrDefault();
        if (firstTaggedElement == null)
            return null;
        // Используем вектор трансляции или нулевой вектор, если он null
        XYZ vector = translationVector2 ?? XYZ.Zero;

        var searchPoint = firstTaggedElement.Position + vector;
        var newTagHead = tagModel.TagHeadPosition + vector;
        var nearestElement = new ElementModel(FindNearestElementOfCategory(_doc, searchPoint, tagModel.TagCategory));
        if (nearestElement.Element == null || nearestElement.Position == null)
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
                    tagModel.TagTypeId, // тип марки
                    _doc?.ActiveView.Id, // id вида
                    new Reference(nearestElement.Element), // ссылка на элемент
                    tagModel.HasLeader, // наличие выноски
                    tagModel.Orientation, // ориентация
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
        newTag.LeaderEndCondition = tagModel.LeaderEndCondition;
        // Если есть выноска, устанавливаем её точки
        if (!tagModel.HasLeader) return newTag;
        foreach (var leader in tagModel.LeadersEnd)
        {
            if (leader.TaggedElement.Id?.Value != firstTaggedElement.Id?.Value) continue;
            try
            {
                if (tagModel.LeaderEndCondition == LeaderEndCondition.Free)
                {
                    newTag.SetLeaderEnd(
                        new Reference(nearestElement.Element),
                        leader.Position.Add(displacement).Add(vector));
                }
            }
            catch (Exception)
            {
                // Пропускаем ошибки при установке конца лидера
            }
        }

        foreach (var leader in tagModel.LeadersElbow)
        {
            if (leader.TaggedElement.Id?.Value != firstTaggedElement.Id?.Value) continue;
            try
            {
                if (tagModel.LeaderEndCondition == LeaderEndCondition.Free)
                {
                    newTag.SetLeaderElbow(
                        new Reference(nearestElement.Element),
                        leader.Position.Add(displacement).Add(vector)
                    );
                }
            }
            catch (Exception)
            {
                // Пропускаем ошибки при установке локтя лидера
            }
        }

        return newTag;
    }

    private Dimension? CreateDimension(DimensionModel dimensionModel, XYZ? translationVector)
    {
        var firstTaggedElement = dimensionModel.References.FirstOrDefault().TaggedElement;
        if (firstTaggedElement == null)
            return null;
        // Используем вектор трансляции или нулевой вектор, если он null
        XYZ vector = translationVector;

        var searchPoint = firstTaggedElement.Position + vector;

        // 1. Получаем ссылки из сохраненной модели
        ReferenceArray references = new ReferenceArray();
        foreach (ReferenceDimensionModel refModel in dimensionModel.References)
        {
            Reference reference = GetReferenceFromModel(_doc, refModel);
            if (reference != null)
            {
                references.Append(reference);
            }
        }

        Line dimensionLine = CreateDimensionLine(dimensionModel.Segments);
        Dimension newDimension = _doc.Create.NewDimension(Context.ActiveView, dimensionLine, references,
            dimensionModel.DimensionType);

        // var newTagHead = dimensionModel.Segments.FirstOrDefault().TextPosition + vector;
        // var nearestElement =
        //     new ElementModel(FindNearestElementOfCategory(_doc, searchPoint, dimensionModel.TagCategory));
        // if (nearestElement.Element == null || nearestElement.Position == null)
        //     return null;
        // var displacement = nearestElement.Position.Subtract(searchPoint);
        // Dimension? newDimension = null;
        // if (ArePointsEqual(searchPoint, nearestElement.Position))
        // {
        //     try
        //     {
        //         newDimension = _doc.Create.NewDimension(Context.ActiveView, dimensionLine, references,
        //             dimensionModel.DimensionType);
        //     }
        //     catch (Exception ex)
        //     {
        //         // Обработка возможных ошибок при создании тега
        //         TaskDialog.Show("Ошибка создания тега", ex.Message);
        //         return null;
        //     }
        // }
        //
        // if (newDimension == null)
        //     return null;

        return newDimension;
    }

    /// <summary>
    /// Получает ссылку из сохраненной модели
    /// </summary>
    private Reference GetReferenceFromModel(Document doc, ReferenceDimensionModel refModel)
    {
        if (refModel.TaggedElement == null || refModel.TaggedElement.Id == null)
            return null;

        // Получаем элемент по Id
        Element element = doc.GetElement(refModel.TaggedElement.Id);
        if (element == null)
            return null;

        // В зависимости от типа ссылки и элемента, получаем соответствующую Reference
        switch (refModel.ElementReferenceType)
        {
            case ElementReferenceType.REFERENCE_TYPE_NONE:
                return new Reference(element);

            case ElementReferenceType.REFERENCE_TYPE_SURFACE:
                // Для поверхностных ссылок пытаемся получить референс от грани
                return new Reference(element);

            case ElementReferenceType.REFERENCE_TYPE_CUT_EDGE:
                // Для рёберных ссылок пытаемся получить референс от ребра
                return new Reference(element);

            default:
                return new Reference(element);
        }
    }

    /// <summary>
    /// Создает линию для размера на основе сохраненных сегментов
    /// </summary>
    private Line CreateDimensionLine(List<DimensionSegmentModel> segments)
    {
        if (segments == null || segments.Count < 1)
            return null;

        // Используем первый и последний сегмент для определения направления линии
        // или точки начала и конца, если сегментов несколько
        XYZ startPoint, endPoint;

        if (segments.Count == 1)
        {
            // Для одного сегмента используем точку Origin и пытаемся определить направление
            startPoint = segments[0].Origin;

            // Примерно определяем конечную точку по TextPosition
            XYZ direction = segments[0].TextPosition - segments[0].Origin;
            direction = direction.Normalize();
            endPoint = startPoint + direction * 5.0; // 5 единиц длина линии размера (примерно)
        }
        else
        {
            // Для нескольких сегментов используем первый и последний
            startPoint = segments.First().Origin;
            endPoint = segments.Last().Origin;
        }

        return Line.CreateBound(startPoint, endPoint);
    }

    private static void SetPositionLeaderElbow(TagModel tagModel, Dictionary<ElementModel, ElementModel> dictionary,
        IndependentTag? newTag, XYZ? translationVector2)
    {
        foreach (var leaderElbow in tagModel.LeadersElbow)
        {
            if (tagModel.LeaderEndCondition != LeaderEndCondition.Free) continue;
            foreach (var kvp in dictionary)
            {
                ElementModel dictionaryKey = kvp.Key;
                Reference? reference = kvp.Value.Reference;

                if (leaderElbow.TaggedElement.Id != dictionaryKey.Element?.Id) continue;
                var searchPoint = leaderElbow.TaggedElement.Position + translationVector2;
                var displacement2 = kvp.Value.Position?.Subtract(searchPoint);
                // Передаем reference из найденной пары ключ-значение
                newTag?.SetLeaderElbow(reference, leaderElbow.Position.Add(displacement2).Add(translationVector2));
                break;
            }
        }
    }

    private static void SetPositionLeaderEnd(TagModel tagModel, Dictionary<ElementModel, ElementModel> dictionary,
        IndependentTag? newTag, XYZ? translationVector2)
    {
        foreach (var leaderEnd in tagModel.LeadersEnd)
        {
            if (tagModel.LeaderEndCondition != LeaderEndCondition.Free) continue;
            foreach (var kvp in dictionary)
            {
                ElementModel dictionaryKey = kvp.Key;
                Reference? reference = kvp.Value.Reference;
                if (leaderEnd.TaggedElement.Id != dictionaryKey.Element?.Id) continue;
                var searchPoint = leaderEnd.TaggedElement.Position + translationVector2;
                var displacement = kvp.Value.Position?.Subtract(searchPoint);
                // Передаем reference из найденной пары ключ-значение
                newTag?.SetLeaderEnd(reference, leaderEnd.Position.Add(displacement).Add(translationVector2));
                break;
            }
        }
    }

    private XYZ? GetTranslationVectorTag(ElementId? copiedTagId, TagModel? originalTag)
    {
        XYZ? translationVector2 = null;
        if (copiedTagId == null) return translationVector2;
        TagModel copyTag = new TagModel(_doc.GetElement(copiedTagId) as IndependentTag);
        if (copyTag == null) return null;

        // Добавляем проверки на null для TagHeadPosition
        if (originalTag?.TagHeadPosition == null || copyTag.TagHeadPosition == null)
        {
            // Если какая-то из позиций равна null, возвращаем null или используем альтернативу
            return null;
        }

        translationVector2 = (originalTag.TagHeadPosition - copyTag.TagHeadPosition).Multiply(-1);
        return translationVector2;
    }

    private XYZ? GetTranslationVectorDimension(ElementId? copiedTagId, DimensionModel? dimensionModel)
    {
        XYZ? translationVector2 = null;
        if (copiedTagId == null) return translationVector2;
        DimensionModel copyDimension = new DimensionModel(_doc.GetElement(copiedTagId) as Dimension);
        if (copyDimension == null) return null;

        // Добавляем проверки на null для TagHeadPosition
        if (dimensionModel?.Segments.First().Origin == null || copyDimension.Segments.First().Origin == null)
        {
            // Если какая-то из позиций равна null, возвращаем null или используем альтернативу
            return null;
        }

        translationVector2 =
            (dimensionModel.Segments.First().Origin - copyDimension.Segments.First().Origin).Multiply(-1);
        return translationVector2;
    }

    private ElementId CopiedTag(Element originalTag, XYZ translationVector)
    {
        ElementId copiedTagId = ElementTransformUtils.CopyElement(
            _doc,
            originalTag.Id,
            translationVector
        ).First();
        return copiedTagId;
    }

    /// <summary>
    /// Проверяет две точки на приблизительное равенство
    /// </summary>
    /// <param name="point1">Первая точка</param>
    /// <param name="point2">Вторая точка</param>
    /// <param name="tolerance">Допустимая погрешность (по умолчанию 0.001)</param>
    /// <returns>True, если точки можно считать равными</returns>
    private bool ArePointsEqual(XYZ? point1, XYZ? point2, double tolerance = 1000)
    {
        if (point1 == null || point2 == null)
            return false;

        // Вычисляем расстояние между точками
        double distance = point1.DistanceTo(point2).ToMillimeters();

        // Возвращаем true, если расстояние меньше или равно заданной погрешности
        return distance <= tolerance;
    }

    private void GetTagsData(List<Reference> selectedTagRefs)
    {
        List<TagModel> tagsData = [];
        List<TextNoteModel> textNoteModels = [];
        foreach (Reference tagRef in selectedTagRefs)
        {
            if (_doc?.GetElement(tagRef) is IndependentTag tag)
            {
                tagsData.Add(new TagModel(tag));
            }

            if (_doc?.GetElement(tagRef) is TextNote textNote)
            {
                textNoteModels.Add(new TextNoteModel(textNote));
            }
        }
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
    /// Находит ближайший элемент указанной категории в пределах заданного расстояния
    /// </summary>
    /// <param name="doc">Текущий документ Revit</param>
    /// <param name="searchPoint">Точка поиска</param>
    /// <param name="category">Категория искомых элементов</param>
    /// <param name="maxSearchDistance">Максимальное расстояние поиска (по умолчанию - 1000 мм)</param>
    /// <returns>Ближайший элемент или null, если ничего не найдено</returns>
    private Element FindNearestElementOfCategory(Document? doc, XYZ searchPoint, BuiltInCategory category,
        ICollection<ElementId?> elementsToExclude = null, double maxSearchDistance = 1000)
    {
        maxSearchDistance = maxSearchDistance.ToInches();
        // Получаем все элементы указанной категории в активном виде
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