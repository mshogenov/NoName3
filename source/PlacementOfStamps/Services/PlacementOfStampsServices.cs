using Autodesk.Revit.DB.Plumbing;
using PlacementOfStamps.Models;

namespace PlacementOfStamps.Services;

public class PlacementOfStampsServices
{
    private const double OffsetStep = 1;
    private const int MaxAttempts = 3;
    private const double SpiralAngleStep = Math.PI / 4;
    private const double MinLeaderLength = 1.5;
    private const double ElbowOffset = 1.0;
    private const double MinTagSpacing = 0.5;

    public void PlacementMarksPipesOuterDiameters(Document doc, List<PipeMdl> pipeMdls,
        View activeView, FamilySymbol selectedTag)
    {
        var elements = pipeMdls.Where(pipe => pipe.IsPipesOuterDiameter);
        // Получаем все существующие марки на активном виде
        var existingAnnotations = GetExistingAnnotations(doc, activeView).Cast<IndependentTag>().ToList();
        var pipeTagsInfo = GetPipeTags(selectedTag, existingAnnotations, activeView);
        List<BoundingBoxXYZ> existingTagBounds = new List<BoundingBoxXYZ>();
        var pipesSortered = elements.OrderBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).X)
            .ThenBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).Y);

        bool flag = false;
        foreach (var pipe in pipesSortered)
        {
            if (pipe.Lenght.ToMillimeters() is > 500 and < 4000 && flag)
            {
                flag = false;
                continue;
            }

            if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
            {
                continue;
            }

//             //Получаем свободное пространство вокруг трубы
//             // Определение центральной точки трубы
//             XYZ midPoint = pipe.Curve.Evaluate(0.5, true);
//             double searchRadius = UnitUtils.ConvertToInternalUnits(5000,UnitTypeId.Millimeters); // Радиус поиска в метрах
//                 //  Создание сферы (если работаете в 3D) или прямоугольника (для 2D-вида)
//                 XYZ minPoint = new XYZ(midPoint.X - searchRadius, midPoint.Y - searchRadius, midPoint.Z);
//                 XYZ maxPoint = new XYZ(midPoint.X + searchRadius, midPoint.Y + searchRadius, midPoint.Z);
//                 // Создание BoundingBoxXYZ для области поиска
//                 BoundingBoxXYZ searchAreaBox = new BoundingBoxXYZ
//                 {
//                     Min = minPoint,
//                     Max = maxPoint,
//                     Transform = Transform.Identity
//                 };
//                 Outline outline = new Outline(searchAreaBox.Min, searchAreaBox.Max);
//                 // Создание фильтра пересечения bounding box
//                 BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(outline);
// List<TagInfo> existingTags = new List<TagInfo>();
// foreach (var existingAnnotation in existingAnnotations)
// {
//     existingTags.Add(new TagInfo(existingAnnotation as IndependentTag)
//     {
//         BoundingBox = existingAnnotation.get_BoundingBox(activeView)
//     });
// }    
// // Сбор элементов, пересекающихся с прямоугольной областью
//             // Определяем категории для труб
//             var pipeCategories = new BuiltInCategory[] { BuiltInCategory.OST_PipeCurves };
//
//             // Определяем категории для аннотаций (можно добавить нужные категории)
//             var annotationCategories = new[]
//             {
//                 BuiltInCategory.OST_GenericAnnotation, 
//                 BuiltInCategory.OST_TextNotes,        
//                 BuiltInCategory.OST_Tags,
//                 BuiltInCategory.OST_PipeTags
//                 
//             };
//
//             // Создаем фильтр по категориям труб и аннотаций
//             ElementMulticategoryFilter categoryFilter = new ElementMulticategoryFilter(pipeCategories.Concat(annotationCategories).ToList());
//
//             // Собираем элементы
//             var collector = new FilteredElementCollector(doc)
//                 .WherePasses(bboxFilter)
//                 .WhereElementIsNotElementType()
//                 .WherePasses(categoryFilter)
//                 .Where(e => e.Id != pipe.Id); // Исключаем выбранную трубу


            XYZ midPoint = pipe.Curve.Evaluate(0.5, true);
            Reference pipeRef = new Reference(pipe.Pipe);
            IndependentTag newTag =
                TryPlaceTagWithMultipleStrategies(doc, activeView, selectedTag, pipeRef, midPoint, existingTagBounds);

            if (newTag != null)
            {
                newTag.LeaderEndCondition = LeaderEndCondition.Free;
                newTag.TagHeadPosition = new XYZ(midPoint.X + 3, midPoint.Y + 2, midPoint.Z);
                existingTagBounds.Add(newTag.get_BoundingBox(activeView));
            }

            // //  Определяем область вокруг точки для проверки свободного пространства
            //   double checkRadius = 0.5; // В метрах, измените по необходимости
            //   BoundingBoxXYZ bbox = new BoundingBoxXYZ
            //   {
            //       Min = new XYZ(midPoint.X - checkRadius, midPoint.Y - checkRadius, 0),
            //       Max = new XYZ(midPoint.X + checkRadius, midPoint.Y + checkRadius, 0)
            //   };
            //   Outline outline = new Outline(bbox.Min, bbox.Max);
            //  // Создаем фильтр для элементов, пересекающих заданную область
            //   BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(outline);
            //  // Шаг 4: Вычисление позиций марки для текущей трубы
            //   List<XYZ> tagLocations =
            //       FindOptimalTagLocation(pipe, selectedTag, pipeTagsInfo);
            //   var collector = new FilteredElementCollector(doc, activeView.Id)
            //       .WherePasses(bboxFilter)
            //       .WhereElementIsNotElementType();
            //   if (tagLocations.Count == 0) continue;
            //  // Создание марки
            //       Reference reference = new Reference(pipe.Pipe);
            //       if (tagLocation == null) continue;
            //       IndependentTag pipeTag = IndependentTag.Create(doc, selectedTag.Id, activeView.Id,
            //           reference, true, TagOrientation.Horizontal, tagLocation);
            //       existingAnnotations.Add(pipeTag);
            //       pipeTag.LeaderEndCondition = LeaderEndCondition.Free;
            //       pipeTag.TagHeadPosition = new XYZ(tagLocation.X + 3, tagLocation.Y + 2, tagLocation.Z);
            //       // pipeTag.SetLeaderElbow(reference,new XYZ(pipeTag.TagHeadPosition.X, pipeTag.TagHeadPosition.Y,pipeTag.TagHeadPosition.Z));
            //   
            flag = true;
        }
    }

    private IndependentTag TryPlaceTagWithMultipleStrategies(Document doc, View view, FamilySymbol tagType,
        Reference pipeRef, XYZ basePoint, List<BoundingBoxXYZ> existingTagBounds)
    {
        // Попробуем разные стратегии размещения
        IndependentTag tag = TryDirectionalPlacement(doc, view, tagType, pipeRef, basePoint, existingTagBounds);
        if (tag != null) return tag;

        tag = TrySpiralPlacement(doc, view, tagType, pipeRef, basePoint, existingTagBounds);
        if (tag != null) return tag;

        tag = TryRandomPlacement(doc, view, tagType, pipeRef, basePoint, existingTagBounds);
        return tag;
    }

    private IndependentTag TryDirectionalPlacement(Document doc, View view, FamilySymbol tagType,
        Reference pipeRef, XYZ basePoint, List<BoundingBoxXYZ> existingTagBounds)
    {
        // Пробуем размещение в 8 основных направлениях
        XYZ[] directions =
        [
            new(1, 0, 0), // Право
            new(-1, 0, 0), // Лево
            new(0, 1, 0), // Верх
            new(0, -1, 0), // Низ
            new(1, 1, 0), // Право-верх
            new(-1, 1, 0), // Лево-верх
            new(1, -1, 0), // Право-низ
            new(-1, -1, 0) // Лево-низ
        ];

        foreach (XYZ direction in directions)
        {
            for (double offset = OffsetStep; offset <= 5; offset += OffsetStep)
            {
                XYZ tagPoint = basePoint + direction * offset;
                IndependentTag newTag = TryCreateTag(doc, view, tagType, pipeRef, tagPoint, existingTagBounds);
                if (newTag != null) return newTag;
            }
        }
        return null;
    }

    private IndependentTag TrySpiralPlacement(Document doc, View view, FamilySymbol tagType,
        Reference pipeRef, XYZ basePoint, List<BoundingBoxXYZ> existingTagBounds)
    {
        double radius = OffsetStep;
        double angle = 0;

        for (int i = 0; i < MaxAttempts; i++)
        {
            double x = radius * Math.Cos(angle);
            double y = radius * Math.Sin(angle);
            XYZ offset = new XYZ(x, y, 0);
            XYZ tagPoint = basePoint + offset;

            IndependentTag newTag = TryCreateTag(doc, view, tagType, pipeRef, tagPoint, existingTagBounds);
            if (newTag != null) return newTag;

            angle += SpiralAngleStep;
            radius += OffsetStep / (2 * Math.PI);
        }

        return null;
    }

    private IndependentTag TryRandomPlacement(Document doc, View view, FamilySymbol tagType,
        Reference pipeRef, XYZ basePoint, List<BoundingBoxXYZ> existingTagBounds)
    {
        Random random = new Random();
        for (int i = 0; i < MaxAttempts; i++)
        {
            double randomAngle = random.NextDouble() * 2 * Math.PI;
            double randomRadius = random.NextDouble() * 5;

            double x = randomRadius * Math.Cos(randomAngle);
            double y = randomRadius * Math.Sin(randomAngle);
            XYZ tagPoint = basePoint + new XYZ(x, y, 0);

            IndependentTag newTag = TryCreateTag(doc, view, tagType, pipeRef, tagPoint, existingTagBounds);
            if (newTag != null) return newTag;
        }

        return null;
    }

    private IndependentTag TryCreateTag(Document doc, View view, FamilySymbol tagType,
        Reference pipeRef, XYZ tagPoint, List<BoundingBoxXYZ> existingTagBounds)
    {
        try
        {
            // Получаем элемент трубы
            Element pipe = doc.GetElement(pipeRef);
            LocationCurve locCurve = pipe.Location as LocationCurve;
            XYZ pipePoint = locCurve.Curve.Evaluate(0.5, true);

            // Создаем метку с выноской
            IndependentTag newTag = IndependentTag.Create(
                doc,
                tagType.Id,
                view.Id,
                pipeRef,
                true, // включаем выноску
                TagOrientation.Horizontal,
                tagPoint
            );

            // Настраиваем выноску
            if (newTag != null)
            {
                // Включаем выноску
                newTag.HasLeader = true;


                // Проверяем наложение
                BoundingBoxXYZ newTagBounds = newTag.get_BoundingBox(view);
                if (!DoesOverlapWithExisting(newTagBounds, existingTagBounds))
                {
                    return newTag;
                }

                doc.Delete(newTag.Id);
            }

            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    private XYZ AdjustTagPosition(XYZ originalPosition, List<BoundingBoxXYZ> existingTagBounds, double minSpacing)
    {
        XYZ adjustedPosition = originalPosition;
        int attempts = 0;
        double offset = minSpacing;

        while (IsPositionConflicting(adjustedPosition, existingTagBounds) && attempts < 50)
        {
            double angle = attempts * (Math.PI / 4);
            adjustedPosition = new XYZ(
                originalPosition.X + offset * Math.Cos(angle),
                originalPosition.Y + offset * Math.Sin(angle),
                0
            );

            attempts++;
            if (attempts % 8 == 0) offset += minSpacing;
        }

        return adjustedPosition;
    }

    private XYZ GetOptimalLeaderPosition(XYZ pipePoint, XYZ tagPoint, double minLength)
    {
        XYZ vector = tagPoint - pipePoint;
        double distance = vector.GetLength();

        if (distance < minLength)
        {
            vector = vector.Normalize();
            return pipePoint + vector * minLength;
        }

        return tagPoint;
    }

    private bool IsPositionConflicting(XYZ position, List<BoundingBoxXYZ> existingTagBounds)
    {
        foreach (BoundingBoxXYZ bounds in existingTagBounds)
        {
            if (IsPointInBox(position, bounds, MinTagSpacing))
                return true;
        }

        return false;
    }

    private bool IsPointInBox(XYZ point, BoundingBoxXYZ box, double tolerance)
    {
        return (point.X >= box.Min.X - tolerance && point.X <= box.Max.X + tolerance &&
                point.Y >= box.Min.Y - tolerance && point.Y <= box.Max.Y + tolerance);
    }


    private bool DoesOverlapWithExisting(BoundingBoxXYZ newBox, List<BoundingBoxXYZ> existingBoxes)
    {
        foreach (BoundingBoxXYZ existingBox in existingBoxes)
        {
            if (DoBoundingBoxesOverlap(newBox, existingBox))
                return true;
        }

        return false;
    }

    private bool DoBoundingBoxesOverlap(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        return !(box1.Max.X < box2.Min.X || box1.Min.X > box2.Max.X ||
                 box1.Max.Y < box2.Min.Y || box1.Min.Y > box2.Max.Y);
    }

    public void PlacementMarksPipeInsulation(Document doc, List<PipeMdl> pipeMdls, View activeView,
        FamilySymbol selectedTag)
    {
        var elements = pipeMdls.Where(pipe => pipe.IsInsulation);
        // Получаем все существующие марки на активном виде
        var existingAnnotations = GetExistingAnnotations(doc, activeView).Cast<IndependentTag>().ToList();
        var pipeTagsInfo = GetPipeTags(selectedTag, existingAnnotations, activeView);

        var pipesSortered = elements.OrderBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).X)
            .ThenBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).Y);

        bool flag = false;
        foreach (var pipe in pipesSortered)
        {
            if (pipe.Lenght.ToMillimeters() is > 500 and < 4000 && flag)
            {
                flag = false;
                continue;
            }

            if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
            {
                continue;
            }

            // Шаг 4: Вычисление позиций марки для текущей трубы
            List<XYZ> tagLocations =
                FindOptimalTagLocation(pipe, selectedTag, pipeTagsInfo);
            if (tagLocations.Count == 0) continue;
            // Создание марки
            foreach (var tagLocation in tagLocations)
            {
                if (tagLocation == null) continue;
                IndependentTag pipeTag = IndependentTag.Create(doc, selectedTag.Id, activeView.Id,
                    new Reference(pipe.Pipe), false, TagOrientation.Horizontal, tagLocation);
                existingAnnotations.Add(pipeTag);
            }

            flag = true;
        }
    }

    public void PlacementMarksSystemAbbreviation(Document doc, List<PipeMdl> pipeModels, View activeView,
        FamilySymbol selectedTag)
    {
        // Получаем все существующие марки на активном виде
        var existingAnnotations = GetExistingAnnotations(doc, activeView)
            .Cast<IndependentTag>()
            .ToList();
        var existingTags = existingAnnotations
            .Select(existingAnnotation => new TagModels(existingAnnotation))
            .ToList();

        var tagModelsEnumerable = existingTags
            .Where(x => x.Name == selectedTag.Name).ToList();
        if (tagModelsEnumerable.Any())
        {
            foreach (var element in pipeModels.ToList())
            {
                foreach (var tag in tagModelsEnumerable)
                {
                    if (tag.TaggedLocalElements.Any(x=>x.Id.Value==element.Id.Value))
                    {
                        pipeModels.Remove(element);
                    }
                }
            }
        }
        var pipeTagsInfo = GetPipeTags(selectedTag, existingAnnotations, activeView);
        var pipesSortered = pipeModels.OrderBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).X)
            .ThenBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).Y);

        bool flag = false;
        foreach (var pipe in pipesSortered)
        {
            if (pipe.Lenght.ToMillimeters() is > 500 and < 4000 && flag)
            {
                flag = false;
                continue;
            }

            if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
            {
                continue;
            }

            // Шаг 4: Вычисление позиций марки для текущей трубы
            List<XYZ> tagLocations =
                FindOptimalTagLocation(pipe, selectedTag, pipeTagsInfo);
            if (tagLocations.Count == 0) continue;
            // Создание марки
            foreach (var tagLocation in tagLocations)
            {
                if (tagLocation == null) continue;
                IndependentTag pipeTag = IndependentTag.Create(doc, selectedTag.Id, activeView.Id,
                    new Reference(pipe.Pipe), false, TagOrientation.Horizontal, tagLocation);
                existingAnnotations.Add(pipeTag);
            }

            flag = true;
        }
    }

    private Dictionary<ElementId, List<TagModels>> GetPipeTags(FamilySymbol selectedTag,
        List<IndependentTag> existingAnnotations, View activeView)
    {
        Dictionary<ElementId, List<TagModels>> pipeTagsInfo = new Dictionary<ElementId, List<TagModels>>();
        // Минимальная допустимая длина кривой для операций
        double shortCurveTolerance = Context.Application.ShortCurveTolerance; // Минимальная длина кривой

        foreach (var element in existingAnnotations)
        {
            // Безопасное приведение элемента к IndependentTag
            if (element is not IndependentTag tag) continue;
            var taggedElements = tag.GetTaggedLocalElements();
            foreach (var taggedElement in taggedElements)
            {
                // Пропустить недействительные элементы или элементы, не являющиеся трубами
                if (taggedElement?.Id == ElementId.InvalidElementId || taggedElement is not Pipe pipe) continue;
                XYZ tagPosition = tag.TagHeadPosition;
                if (pipe.Location is not LocationCurve pipeLocation) continue;
                Curve pipeCurve = pipeLocation.Curve;
                IntersectionResult result = pipeCurve.Project(tagPosition);
                double parameter = result.Parameter;

                if (!pipeTagsInfo.ContainsKey(pipe.Id))
                {
                    pipeTagsInfo[pipe.Id] = [];
                }

                BoundingBoxXYZ tagBoundingBox = tag.get_BoundingBox(activeView);
                double startParam = pipeCurve.GetEndParameter(0);
                double endParam = parameter;

                // Проверка порядка параметров
                if (startParam > endParam)
                {
                    (startParam, endParam) = (endParam, startParam);
                }

                // Проверяем, чтобы параметры имели допустимое расстояние
                double distanceAlongCurve;

                if (Math.Abs(startParam - endParam) < shortCurveTolerance)
                {
                    // Если параметры слишком близки, вычисляем расстояние напрямую
                    XYZ startPoint = pipeCurve.Evaluate(startParam, false);
                    XYZ endPoint = pipeCurve.Evaluate(endParam, false);
                    distanceAlongCurve = startPoint.DistanceTo(endPoint);
                }
                else
                {
                    // Если параметры корректны, ограничиваем кривую
                    Curve partialCurve = pipeCurve.Clone();
                    try
                    {
                        partialCurve.MakeBound(startParam, endParam);
                        distanceAlongCurve = partialCurve.Length;
                    }
                    catch
                    {
                        continue;
                    }
                }

                pipeTagsInfo[pipe.Id].Add(new TagModels(tag)
                {
                    BoundingBox = tagBoundingBox,
                    Distance = distanceAlongCurve,
                });
            }
        }

        return pipeTagsInfo;
    }

    /// <summary>
    /// Получает список существующих аннотаций в текущем представлении
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="view">Текущее представление</param>
    /// <returns>Список элементов аннотаций</returns>
    private IEnumerable<Element> GetExistingAnnotations(Document doc, View view)
    {
        // Получаем IndependentTags и TextNotes в текущем представлении
        return new FilteredElementCollector(doc, view.Id)
            .OfClass(typeof(IndependentTag))
            .WhereElementIsNotElementType();
    }

    /// <summary>
    /// Находит оптимальную позицию для размещения марки, избегая перекрытий
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="view">Текущее представление</param>
    /// <param name="pipe">Труба для маркировки</param>
    /// <param name="tagSymbol">Семейство символа тега</param>
    /// <param name="existingAnnotations">Список существующих аннотаций для проверки перекрытий</param>
    /// <param name="pipeTaggedPositions"></param>
    /// <returns>Позиция для размещения марки или null, если подходящая позиция не найдена</returns>
    private List<XYZ> FindOptimalTagLocation(PipeMdl pipe, FamilySymbol tagSymbol,
        Dictionary<ElementId, List<TagModels>> pipeTaggedPositions)
    {
        // Конвертируйте в футы (внутренняя единица Revit)
        double interval = UnitUtils.ConvertToInternalUnits(3000, UnitTypeId.Millimeters);
        // Вычисляем количество марок
        int numberOfTags = (int)(pipe.Lenght / interval);
        if (pipe.Lenght % interval != 0) numberOfTags++;

        // Получаем список существующих марок на этой трубе
        List<TagModels> existingTags = [];
        if (pipeTaggedPositions.TryGetValue(pipe.Id, out var pos))
        {
            existingTags = pos;
        }

        if (pipe.Lenght < interval && pipe.Lenght > UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters))
        {
            List<XYZ> position =
            [
                GetPosition(tagSymbol, existingTags, pipe)
            ];
            return position;
        }

        return GetPositionInterval(tagSymbol, numberOfTags, interval, existingTags, pipe);
    }


    private List<XYZ> GetPositionInterval(FamilySymbol tagSymbol, int numberOfTags, double interval,
        List<TagModels> existingTags, PipeMdl pipe)
    {
        List<XYZ> positions = [];
        // Итерация по количеству марок
        for (int i = 1; i < numberOfTags; i++)
        {
            double distance = i * interval;
            // Ограничиваем расстояние длиной трубы
            if (distance > pipe.Lenght)
                distance = pipe.Lenght;
            // Вычисляем позицию марки
            XYZ tagPoint;
            XYZ point = pipe.StartPoint + pipe.Direction * distance;
            if (pipe.IsDisplaced)
            {
                tagPoint = new XYZ(point.X + pipe.PointDisplaced.X, point.Y + pipe.PointDisplaced.Y,
                    point.Z + pipe.PointDisplaced.Z);
            }
            else
            {
                tagPoint = point;
            }

            // Проверяем наличие марки в этой позиции
            bool sameTagExists = existingTags.Any(tagInfo =>
                Math.Abs(tagInfo.Distance - distance) < interval &&
                tagInfo.Name == tagSymbol.Name);

            if (sameTagExists)
            {
                // Такая же марка уже существует на этой позиции, пропускаем
                return null;
            }

            // Проверяем наличие марки другого типа
            TagModels existingTag = null;
            bool differentTagExists = existingTags.Any(tagInfo =>
            {
                if (!(Math.Abs(tagInfo.Distance - distance) < interval) ||
                    tagInfo.Name == tagSymbol.Name) return false;
                existingTag = tagInfo;
                return true;
            });
            if (differentTagExists)
            {
                // Марка другого типа существует на этой позиции, необходимо сместить новую марку
                XYZ shiftedTagPoint = FindFreeTagPosition(tagPoint, existingTag, pipe);
                if (shiftedTagPoint != null)
                {
                    positions.Add(shiftedTagPoint);
                }
            }
            else
            {
                // Марки в этой позиции нет, можно размещать без смещения
                positions.Add(tagPoint);
            }
        }

        return positions;
    }


    private XYZ GetPosition(FamilySymbol tagSymbol, List<TagModels> existingTags, PipeMdl pipe)
    {
        XYZ tagPoint;

        XYZ point = (pipe.StartPoint + pipe.EndPoint) / 2;
        if (pipe.IsDisplaced)
        {
            tagPoint = new XYZ(point.X + pipe.PointDisplaced.X, point.Y + pipe.PointDisplaced.Y,
                point.Z + pipe.PointDisplaced.Z);
        }
        else
        {
            tagPoint = point;
        }


        XYZ position = new XYZ();
        // Проверяем наличие марки в этой позиции
        bool sameTagExists = existingTags.Any(tagInfo =>
        {
            XYZ annotationPoint = tagInfo.TagElement.TagHeadPosition;
            double distance = tagPoint.DistanceTo(annotationPoint);
            return distance < 1 && tagInfo.Name == tagSymbol.Name;
        });

        if (sameTagExists)
        {
            // Такая же марка уже существует на этой позиции, пропускаем
            return position;
        }

        TagModels existingTag = null;
        // Проверяем наличие марки другого типа
        bool differentTagExists = existingTags.Any(tagInfo =>
        {
            XYZ annotationPoint = tagInfo.TagElement.TagHeadPosition;
            double distance = tagPoint.DistanceTo(annotationPoint);
            if (distance < UnitUtils.ConvertToInternalUnits(400, UnitTypeId.Millimeters) &&
                tagInfo.Name != tagSymbol.Name)
            {
                existingTag = tagInfo;
                return true;
            }

            return false;
        });

        if (differentTagExists)
        {
            // Марка другого типа существует на этой позиции, необходимо сместить новую марку
            XYZ shiftedTagPoint = FindFreeTagPosition(tagPoint, existingTag, pipe);
            if (shiftedTagPoint != null)
            {
                position = shiftedTagPoint;
            }
            else
            {
                return null;
            }
        }
        else
        {
            // Марки в этой позиции нет, можно размещать без смещения
            position = tagPoint;
        }

        return position;
    }

    // Метод для поиска свободной позиции для марки
    private XYZ FindFreeTagPosition(XYZ originalPosition, TagModels existingTag, PipeMdl pipe)
    {
        // Максимальное количество попыток смещения для каждой позиции
        int maxAttempts = 1;
        // Направление смещения (параллельное направлению трубы в плоскости XY)
        XYZ shiftDirection = GetParallelDirectionXy(pipe);
        double exclusionTolerance = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters);
        // Дополнительный зазор для предотвращения наложений (в внутренних единицах Revit, обычно футы)
        double additionalGap = UnitUtils.ConvertToInternalUnits(400, UnitTypeId.Millimeters);
        // Получаем габариты тега
        BoundingBoxXYZ boundingBox = existingTag.BoundingBox;
        XYZ min = boundingBox.Min;
        XYZ max = boundingBox.Max;
        double width = (max.X - min.X);
        double height = (max.Y - min.Y);
        double shiftStep = width > height ? width : height;


        // Попытки смещения вправо и влево
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // Смещение вправо
            XYZ shiftedPositionRight = originalPosition + shiftDirection * (shiftStep * attempt + additionalGap);
            if (IsPositionWithinPipeBounds(shiftedPositionRight, pipe, exclusionTolerance) &&
                !IsPositionOccupied(shiftedPositionRight, existingTag, pipe))
            {
                return shiftedPositionRight;
            }

            // Смещение влево
            XYZ shiftedPositionLeft = originalPosition - shiftDirection * (shiftStep * attempt + additionalGap);
            if (IsPositionWithinPipeBounds(shiftedPositionLeft, pipe, exclusionTolerance) &&
                !IsPositionOccupied(shiftedPositionLeft, existingTag, pipe))
            {
                return shiftedPositionLeft;
            }
        }

        // Если свободное место не найдено, возвращаем null
        return null;
    }

    private bool IsPositionWithinPipeBounds(XYZ position, PipeMdl pipe, double exclusionTolerance)
    {
        // Проецируем позицию на кривую трубы
        IntersectionResult result = pipe.Curve.Project(position);

        // Если проекция недействительна, позиция вне кривой
        if (result == null) return false;

        // Получаем параметр проекции
        double param = result.Parameter;

        // Получаем параметры начала и конца трубы
        double startParam = pipe.Curve.GetEndParameter(0);
        double endParam = pipe.Curve.GetEndParameter(1);

        // Убедимся, что startParam меньше endParam
        if (startParam > endParam)
        {
            (startParam, endParam) = (endParam, startParam);
        }

        // Проверяем, чтобы параметр находился в пределах кривой, исключая начало и конец
        return param > startParam + exclusionTolerance && param < endParam - exclusionTolerance;
    }

    // Метод для проверки, занята ли позиция маркой
    private bool IsPositionOccupied(XYZ position, TagModels existingTag, PipeMdl pipe)
    {
        // Вычисляем расстояние по параметру кривой до существующей марки
        double existingDistance = existingTag.Parameter;
        double currentDistance = pipe.Curve.Project(position).Parameter;

        // Проверяем близость по параметрам
        return Math.Abs(existingDistance - currentDistance) < 0.1 &&
               BoundingBoxesOverlap(existingTag.BoundingBox, position);
    }

    private bool BoundingBoxesOverlap(BoundingBoxXYZ existingBox, XYZ newTagPosition)
    {
        // Предполагаем, что новая марка имеет определённые размеры
        // Можно получить размеры из семейства марки или задать фиксированные значения
        double newTagWidth = UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters);
        double newTagHeight = UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters);

        // Создаём BoundingBox для новой марки вокруг её позиции
        BoundingBoxXYZ newTagBox = new BoundingBoxXYZ
        {
            Min = new XYZ(newTagPosition.X - newTagWidth / 2, newTagPosition.Y - newTagHeight / 2, newTagPosition.Z),
            Max = new XYZ(newTagPosition.X + newTagWidth / 2, newTagPosition.Y + newTagHeight / 2,
                newTagPosition.Z + 0.5)
        };

        // Получаем BoundingBox существующей марки
        BoundingBoxXYZ existingBounds = existingBox;

        // Создаём объекты Outline для проверки пересечения
        Outline outline1 = new Outline(existingBounds.Min, existingBounds.Max);
        Outline outline2 = new Outline(newTagBox.Min, newTagBox.Max);

        return outline1.Intersects(outline2, 0);
    }

    private XYZ GetPerpendicularDirection(Curve curve)
    {
        // Получаем направление трубы
        XYZ startPt = curve.GetEndPoint(0);
        XYZ endPt = curve.GetEndPoint(1);
        XYZ direction = (endPt - startPt).Normalize();

        // Вычисляем перпендикулярное направление в плоскости XY
        return new XYZ(-direction.Y, direction.X, 0).Normalize();
    }

    private XYZ GetParallelDirectionXy(PipeMdl pipe)
    {
        // Проецируем направление на плоскость XY (обнуляем Z-компонент)
        XYZ directionXy = new XYZ(pipe.Direction.X, pipe.Direction.Y, 0);

        // Проверяем, что направление не нулевое после проекции
        if (directionXy.IsZeroLength())
        {
            // Если направление вертикальное (Z-направление), устанавливаем направление по умолчанию
            directionXy = XYZ.BasisX;
        }
        else
        {
            // Нормализуем направление в плоскости XY
            directionXy = directionXy.Normalize();
        }

        // Возвращаем параллельное направление в плоскости XY
        return directionXy;
    }

    // Метод для смещения точки вдоль кривой
    private XYZ ShiftAlongCurve(Curve curve, XYZ point, double shift)
    {
        // Получаем параметры начала и конца кривой
        double startParam = curve.GetEndParameter(0);
        double endParam = curve.GetEndParameter(1);

        // Проецируем точку на кривую, чтобы получить параметр
        IntersectionResult result = curve.Project(point);
        double parameter = result.Parameter;

        // Вычисляем длину кривой
        double curveLength = curve.Length;

        // Вычисляем отношение параметра к общей длине параметров
        double paramRatio = (parameter - startParam) / (endParam - startParam);

        // Вычисляем фактическое расстояние вдоль кривой до текущего параметра
        double lengthAtParam = paramRatio * curveLength;

        // Смещаем длину вдоль кривой
        double shiftedLength = lengthAtParam + shift;

        // Ограничиваем смещённую длину между 0 и длиной кривой
        if (shiftedLength < 0) shiftedLength = 0;
        if (shiftedLength > curveLength) shiftedLength = curveLength;

        // Вычисляем новое отношение параметра
        double newParamRatio = shiftedLength / curveLength;

        // Получаем новый параметр кривой
        double newParameter = startParam + newParamRatio * (endParam - startParam);

        // Получаем новую точку на кривой
        XYZ newPoint = curve.Evaluate(newParameter, false);

        return newPoint;
    }


    /// <summary>
    /// Проверяет, существует ли уже марка в пределах заданной толерантности от указанной позиции.
    /// </summary>
    /// <param name="annotation">Список существующих аннотаций (марки и текстовые заметки).</param>
    /// <param name="tagSymbol"></param>
    /// <param name="candidateLocation">Кандидатная позиция для размещения марки.</param>
    /// <returns>True, если марка уже существует в пределах толерантности, иначе False.</returns>
    private bool IsTagAlreadyPresent(Element annotation, FamilySymbol tagSymbol, XYZ candidateLocation)
    {
        IndependentTag tag = annotation as IndependentTag;
        XYZ annotationPoint = tag?.TagHeadPosition;
        double distance = candidateLocation.DistanceTo(annotationPoint);
        if (distance <= 0.1 && tagSymbol.Name == annotation.Name)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Определяет размеры марки из семейства.
    /// </summary>
    /// <param name="tagSymbol">FamilySymbol марки.</param>
    /// <returns>Кортеж с шириной и высотой марки.</returns>
    private (double width, double height) GetTagSize(FamilySymbol tagSymbol)
    {
        // Предопределенные размеры в футах (пример: 100 мм x 50 мм)
        double width = 100.0 / 304.8; // 100 мм в футах
        double height = 50.0 / 304.8; // 50 мм в футах
        return (width, height);
    }

    /// <summary>
    /// Создает BoundingBoxXYZ для марки на основе центра, ширины и высоты
    /// </summary>
    /// <param name="center">Центр марки</param>
    /// <param name="width">Ширина марки</param>
    /// <param name="height">Высота марки</param>
    /// <param name="view">Текущее представление</param>
    /// <returns>Ограничивающий прямоугольник</returns>
    private BoundingBoxXYZ CreateBoundingBox(XYZ center, double width, double height, View view)
    {
        BoundingBoxXYZ bbox = new BoundingBoxXYZ();
        // Устанавливаем максимальную и минимальную точки BoundingBox
        // Здесь предполагается, что марка размещается в горизонтальной плоскости
        bbox.Min = center - new XYZ(width / 2, height / 2, 0);
        bbox.Max = center + new XYZ(width / 2, height / 2, 0);
        return bbox;
    }

    /// <summary>
    /// Проверяет, занята ли позиция элементами в представлении
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="view">Текущее представление</param>
    /// <param name="bbox">BoundingBoxXYZ проверяемой области</param>
    /// <returns>True, если занято, иначе False</returns>
    private bool IsLocationOccupied(Document doc, View view, BoundingBoxXYZ bbox)
    {
        // Создаем Outline из BoundingBoxXYZ
        Outline outline = new Outline(bbox.Min, bbox.Max);

        // Создаем BoundingBoxIntersectsFilter с созданным Outline
        BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);

        // Получаем все элементы в представлении, попадающие в bounding box
        var collector = new FilteredElementCollector(doc, view.Id)
            .WherePasses(filter)
            .WhereElementIsNotElementType()
            // Исключаем IndependentTag и TextNote, чтобы не проверять на пересечения сами с собой
            .Where(e => !(e is IndependentTag) && !(e is TextNote));

        return collector.Any();
    }
}