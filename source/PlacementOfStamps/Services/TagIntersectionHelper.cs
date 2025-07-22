using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using PlacementOfStamps.Models;

namespace PlacementOfStamps.Services;

public static class TagIntersectionHelper
{
    /// <summary>
    /// Получает список элементов, пересекающихся с bounding box марки
    /// </summary>
    /// <param name="document">Документ Revit</param>
    /// <param name="independentTag">Марка для проверки пересечений</param>
    /// <param name="view">Вид, на котором размещена марка</param>
    /// <param name="excludeTaggedElement">Исключить помеченный элемент из результата</param>
    /// <returns>Список пересекающихся элементов</returns>
    public static List<Element> GetIntersectingElements(
        Document document,
        IndependentTag independentTag,
        View view,
        bool excludeTaggedElement = true)
    {
        if (document == null || independentTag == null || view == null)
            return new List<Element>();

        try
        {
            // Получаем bounding box марки
            BoundingBoxXYZ tagBoundingBox = independentTag.get_BoundingBox(view);

            if (tagBoundingBox == null)
                return new List<Element>();

            // Создаем Outline для фильтрации
            Outline outline = new Outline(tagBoundingBox.Min, tagBoundingBox.Max);

            // Создаем фильтр пересечения
            BoundingBoxIntersectsFilter intersectionFilter = new BoundingBoxIntersectsFilter(outline);

            // Фильтр для исключения самой марки
            ElementId tagId = independentTag.Id;
            List<ElementId> excludeIds = new List<ElementId> { tagId };



            ExclusionFilter exclusionFilter = new ExclusionFilter(excludeIds);

            // Комбинируем фильтры
            LogicalAndFilter combinedFilter = new LogicalAndFilter(intersectionFilter, exclusionFilter);

            // Собираем элементы
            FilteredElementCollector collector = new FilteredElementCollector(document, view.Id)
                .WherePasses(combinedFilter);

            return collector.ToList();
        }
        catch (System.Exception ex)
        {
            // Логирование ошибки
            TaskDialog.Show("Ошибка", $"Ошибка при получении пересекающихся элементов: {ex.Message}");
            return new List<Element>();
        }
    }

    /// <summary>
    /// Получает список элементов определенной категории, пересекающихся с bounding box марки
    /// </summary>
    /// <param name="document">Документ Revit</param>
    /// <param name="independentTag">Марка для проверки пересечений</param>
    /// <param name="view">Вид, на котором размещена марка</param>
    /// <param name="categories">Список категорий для фильтрации</param>
    /// <param name="excludeTaggedElement">Исключить помеченный элемент из результата</param>
    /// <returns>Список пересекающихся элементов указанных категорий</returns>
    public static List<Element> GetIntersectingElementsByCategory(
        Document document,
        IndependentTag independentTag,
        View view,
        List<BuiltInCategory> categories,
        bool excludeTaggedElement = true)
    {
        if (document == null || independentTag == null || view == null || categories == null || !categories.Any())
            return new List<Element>();

        try
        {
            // Получаем bounding box марки
            BoundingBoxXYZ tagBoundingBox = independentTag.get_BoundingBox(view);

            if (tagBoundingBox == null)
                return new List<Element>();

            // Создаем Outline для фильтрации
            Outline outline = new Outline(tagBoundingBox.Min, tagBoundingBox.Max);

            // Создаем фильтр пересечения
            BoundingBoxIntersectsFilter intersectionFilter = new BoundingBoxIntersectsFilter(outline);

            // Создаем фильтр категорий
            List<ElementFilter> categoryFilters = categories
                .Select(cat => new ElementCategoryFilter(cat) as ElementFilter)
                .ToList();

            LogicalOrFilter categoryFilter = new LogicalOrFilter(categoryFilters);

            // Фильтр для исключения самой марки
            ElementId tagId = independentTag.Id;
            List<ElementId> excludeIds = new List<ElementId> { tagId };


            ExclusionFilter exclusionFilter = new ExclusionFilter(excludeIds);

            // Комбинируем все фильтры
            LogicalAndFilter combinedFilter = new LogicalAndFilter(
                new List<ElementFilter> { intersectionFilter, categoryFilter, exclusionFilter });

            // Собираем элементы
            FilteredElementCollector collector = new FilteredElementCollector(document, view.Id)
                .WherePasses(combinedFilter);

            return collector.ToList();
        }
        catch (System.Exception ex)
        {
            // Логирование ошибки
            TaskDialog.Show("Ошибка", $"Ошибка при получении пересекающихся элементов по категории: {ex.Message}");
            return new List<Element>();
        }
    }


    /// Получает список марок, пересекающихся с указанной маркой на виде

    /// <param name="document">Документ Revit</param>
    /// <param name="independentTag">Марка для проверки пересечений</param>
    /// <param name="view">Вид, на котором размещена марка</param>
    /// <returns>Список пересекающихся марок</returns>
    public static List<IndependentTag> GetIntersectingTags(
        Document document,
        IndependentTag independentTag,
        View view)
    {
        if (document == null || independentTag == null || view == null)
            return new List<IndependentTag>();

        try
        {
            // Получаем все марки на виде
            var collector = new FilteredElementCollector(document, view.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .Where(tag => tag.Id != independentTag.Id) // Исключаем саму марку
                .ToList();

            List<IndependentTag> intersectingTags = new List<IndependentTag>();

            // Получаем bounding box исходной марки
            BoundingBoxXYZ sourceBoundingBox = GetTagBoundingBoxOnView(independentTag, view);
            if (sourceBoundingBox == null)
                return intersectingTags;

            // Проверяем каждую марку на пересечение
            foreach (IndependentTag otherTag in collector)
            {
                BoundingBoxXYZ otherBoundingBox = GetTagBoundingBoxOnView(otherTag, view);
                if (otherBoundingBox != null && BoundingBoxesIntersect2D(sourceBoundingBox, otherBoundingBox))
                {
                    intersectingTags.Add(otherTag);
                }
            }

            return intersectingTags;
        }
        catch (System.Exception ex)
        {
            TaskDialog.Show("Ошибка", $"Ошибка при получении пересекающихся марок: {ex.Message}");
            return new List<IndependentTag>();
        }
    }

    /// <summary>
    /// Получает корректный bounding box марки на виде
    /// </summary>
    /// <param name="tag">Марка</param>
    /// <param name="view">Вид</param>
    /// <returns>BoundingBox или null</returns>
    private static BoundingBoxXYZ GetTagBoundingBoxOnView(IndependentTag tag, View view)
    {
        try
        {
            // Сначала пробуем получить стандартный bounding box
            BoundingBoxXYZ boundingBox = tag.get_BoundingBox(view);

            if (boundingBox != null)
                return boundingBox;

            // Если стандартный способ не работает, пробуем через геометрию
            Options geometryOptions = new Options();
            geometryOptions.View = view;
            geometryOptions.IncludeNonVisibleObjects = false;
            geometryOptions.ComputeReferences = false;

            GeometryElement geometryElement = tag.get_Geometry(geometryOptions);
            if (geometryElement != null)
            {
                BoundingBoxXYZ geomBoundingBox = geometryElement.GetBoundingBox();
                if (geomBoundingBox != null)
                    return geomBoundingBox;
            }

            // Последний способ - через положение марки и примерный размер
            XYZ tagPosition = tag.TagHeadPosition;
            if (tagPosition != null)
            {
                // Создаем примерный bounding box размером 1x0.5 фута вокруг позиции марки
                double width = 1.0; // 1 фут
                double height = 0.5; // 0.5 фута

                BoundingBoxXYZ approximateBoundingBox = new BoundingBoxXYZ();
                approximateBoundingBox.Min = new XYZ(
                    tagPosition.X - width / 2,
                    tagPosition.Y - height / 2,
                    tagPosition.Z - 0.1);
                approximateBoundingBox.Max = new XYZ(
                    tagPosition.X + width / 2,
                    tagPosition.Y + height / 2,
                    tagPosition.Z + 0.1);

                return approximateBoundingBox;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
    /// <summary>
    /// Упрощенный метод получения пересекающихся труб из существующей коллекции
    /// </summary>
    /// <param name="tag">Марка</param>
    /// <param name="currentPipe">Текущая труба (исключается)</param>
    /// <param name="allPipes">Все трубы в проекте</param>
    /// <param name="view">Активный вид</param>
    /// <returns>Список пересекающихся труб</returns>
    private static List<Pipe> GetIntersectingPipesFromCollection(TagWrp tag, PipeWrp currentPipe, 
        List<PipeWrp> allPipes, View view)
    {
        List<Pipe> intersectingPipes = new List<Pipe>();

        BoundingBoxXYZ tagBoundingBox = tag.IndependentTag.get_BoundingBox(view);
        if (tagBoundingBox == null) return intersectingPipes;

        foreach (var pipeWrp in allPipes)
        {
            // Пропускаем текущую трубу
            if (pipeWrp.Pipe.Id == currentPipe.Pipe.Id) continue;

            BoundingBoxXYZ pipeBoundingBox = pipeWrp.Pipe.get_BoundingBox(view);
            if (pipeBoundingBox != null && BoundingBoxesIntersect2D(tagBoundingBox, pipeBoundingBox))
            {
                intersectingPipes.Add(pipeWrp.Pipe);
            }
        }

        return intersectingPipes;
    }
    /// <summary>
    /// Проверяет пересечение двух bounding box в 2D (игнорируя Z координату)
    /// </summary>
    /// <param name="box1">Первый bounding box</param>
    /// <param name="box2">Второй bounding box</param>
    /// <returns>True если пересекаются</returns>
    private static bool BoundingBoxesIntersect2D(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        if (box1 == null || box2 == null)
            return false;

        // Проверяем пересечение по X
        bool xIntersects = box1.Min.X <= box2.Max.X && box1.Max.X >= box2.Min.X;

        // Проверяем пересечение по Y
        bool yIntersects = box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y;

        return xIntersects && yIntersects;
    }

    /// <summary>
    /// Альтернативный метод через геометрическое пересечение
    /// </summary>
    /// <param name="document">Документ Revit</param>
    /// <param name="independentTag">Марка для проверки пересечений</param>
    /// <param name="view">Вид, на котором размещена марка</param>
    /// <returns>Список пересекающихся марок</returns>
    public static List<IndependentTag> GetIntersectingTagsGeometric(
        Document document,
        IndependentTag independentTag,
        View view)
    {
        if (document == null || independentTag == null || view == null)
            return new List<IndependentTag>();

        try
        {
            List<IndependentTag> intersectingTags = new List<IndependentTag>();

            // Получаем все марки на виде
            var allTags = new FilteredElementCollector(document, view.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .Where(tag => tag.Id != independentTag.Id)
                .ToList();

            // Получаем Solid исходной марки
            Solid sourceSolid = GetTagSolid(independentTag, view);
            if (sourceSolid == null)
                return intersectingTags;

            foreach (IndependentTag otherTag in allTags)
            {
                Solid otherSolid = GetTagSolid(otherTag, view);
                if (otherSolid != null)
                {
                    try
                    {
                        // Проверяем пересечение через BooleanOperations
                        Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                            sourceSolid, otherSolid, BooleanOperationsType.Intersect);

                        if (intersection != null && intersection.Volume > 0)
                        {
                            intersectingTags.Add(otherTag);
                        }
                    }
                    catch
                    {
                        // Если геометрическое пересечение не работает, используем bounding box
                        BoundingBoxXYZ sourceBB = sourceSolid.GetBoundingBox();
                        BoundingBoxXYZ otherBB = otherSolid.GetBoundingBox();

                        if (BoundingBoxesIntersect2D(sourceBB, otherBB))
                        {
                            intersectingTags.Add(otherTag);
                        }
                    }
                }
            }

            return intersectingTags;
        }
        catch (System.Exception ex)
        {
            TaskDialog.Show("Ошибка", $"Ошибка при геометрической проверке пересечений: {ex.Message}");
            return new List<IndependentTag>();
        }
    }

    /// <summary>
    /// Получает Solid марки для геометрических операций
    /// </summary>
    /// <param name="tag">Марка</param>
    /// <param name="view">Вид</param>
    /// <returns>Solid или null</returns>
    private static Solid GetTagSolid(IndependentTag tag, View view)
    {
        try
        {
            BoundingBoxXYZ boundingBox = GetTagBoundingBoxOnView(tag, view);
            if (boundingBox == null)
                return null;

            return CreateBoxSolid(boundingBox);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Создает Solid в форме бокса из BoundingBox
    /// </summary>
    /// <param name="boundingBox">BoundingBox для создания Solid</param>
    /// <returns>Solid или null</returns>
    private static Solid CreateBoxSolid(BoundingBoxXYZ boundingBox)
    {
        if (boundingBox == null)
            return null;

        try
        {
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;

            // Нормализуем координаты
            double minX = System.Math.Min(min.X, max.X);
            double maxX = System.Math.Max(min.X, max.X);
            double minY = System.Math.Min(min.Y, max.Y);
            double maxY = System.Math.Max(min.Y, max.Y);
            double minZ = System.Math.Min(min.Z, max.Z);
            double maxZ = System.Math.Max(min.Z, max.Z);

            // Проверяем размеры
            const double tolerance = 1e-9;
            double width = maxX - minX;
            double depth = maxY - minY;
            double height = maxZ - minZ;

            if (width < tolerance) width = 0.01; // Минимальная ширина
            if (depth < tolerance) depth = 0.01; // Минимальная глубина
            if (height < tolerance) height = 0.01; // Минимальная высота

            // Создаем прямоугольный профиль
            XYZ p1 = new XYZ(minX, minY, minZ);
            XYZ p2 = new XYZ(minX + width, minY, minZ);
            XYZ p3 = new XYZ(minX + width, minY + depth, minZ);
            XYZ p4 = new XYZ(minX, minY + depth, minZ);

            CurveLoop curveLoop = new CurveLoop();
            curveLoop.Append(Line.CreateBound(p1, p2));
            curveLoop.Append(Line.CreateBound(p2, p3));
            curveLoop.Append(Line.CreateBound(p3, p4));
            curveLoop.Append(Line.CreateBound(p4, p1));

            List<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };

            return GeometryCreationUtilities.CreateExtrusionGeometry(
                curveLoops,
                XYZ.BasisZ,
                height);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Получает трубы, которые пересекаются с маркой (исключая текущую трубу)
    /// </summary>
    /// <param name="tag">Марка для проверки</param>
    /// <param name="currentPipe">Текущая труба (исключается из результата)</param>
    /// <param name="view">Активный вид</param>
    /// <returns>Список пересекающихся труб</returns>
    public static List<Pipe> GetIntersectingPipes(TagWrp tag, PipeWrp currentPipe, View view)
    {
        List<Pipe> intersectingPipes = new List<Pipe>();

        try
        {
            // Получаем BoundingBox марки
            BoundingBoxXYZ tagBoundingBox = tag.IndependentTag.get_BoundingBox(view);
            if (tagBoundingBox == null) return intersectingPipes;

            // Создаем Outline для фильтрации
            Outline outline = new Outline(tagBoundingBox.Min, tagBoundingBox.Max);

            // Фильтр для труб
            ElementCategoryFilter pipeFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);

            // Фильтр по BoundingBox
            BoundingBoxIntersectsFilter boundingBoxFilter = new BoundingBoxIntersectsFilter(outline);

            // Комбинированный фильтр
            LogicalAndFilter combinedFilter = new LogicalAndFilter(pipeFilter, boundingBoxFilter);

            // Получаем все трубы в области марки
            FilteredElementCollector collector = new FilteredElementCollector(view.Document, view.Id)
                .WherePasses(combinedFilter);

            foreach (Element element in collector)
            {
                if (element is Pipe pipe && pipe.Id != currentPipe.Pipe.Id)
                {
                    // Дополнительная проверка пересечения
                    if (DoesPipeIntersectWithTag(pipe, tag, view))
                    {
                        intersectingPipes.Add(pipe);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Логирование ошибки
            TaskDialog.Show("Ошибка", $"Ошибка при поиске пересекающихся труб: {ex.Message}");
        }

        return intersectingPipes;
    }
    /// <summary>
    /// Проверяет, пересекается ли труба с маркой
    /// </summary>
    /// <param name="pipe">Труба для проверки</param>
    /// <param name="tag">Марка</param>
    /// <param name="view">Вид</param>
    /// <returns>True если пересекаются</returns>
    private static bool DoesPipeIntersectWithTag(Pipe pipe, TagWrp tag, View view)
    {
        try
        {
            // Получаем BoundingBox трубы и марки
            BoundingBoxXYZ pipeBoundingBox = pipe.get_BoundingBox(view);
            BoundingBoxXYZ tagBoundingBox = tag.IndependentTag.get_BoundingBox(view);

            if (pipeBoundingBox == null || tagBoundingBox == null)
                return false;

            // Проверяем пересечение BoundingBox в 2D
            return BoundingBoxesIntersect2D(pipeBoundingBox, tagBoundingBox);
        }
        catch
        {
            return false;
        }
    }
}
