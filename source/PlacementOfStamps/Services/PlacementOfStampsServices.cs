using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using PlacementOfStamps.Models;
using Rectangle = PlacementOfStamps.Models.Rectangle;

namespace PlacementOfStamps.Services;

public class PlacementOfStampsServices
{
    private readonly Document _doc = Context.ActiveDocument;
    private const double OffsetStep = 1;
    private const int MaxAttempts = 3;
    private const double SpiralAngleStep = Math.PI / 4;
    private const double MinLeaderLength = 1.5;
    private const double ElbowOffset = 1.0;
    private const double MinTagSpacing = 0.5;

//     public void PlacementMarksPipesOuterDiameters(Document doc, List<PipeMdl> pipeMdls,
//         View activeView, FamilySymbol selectedTag)
//     {
//         var elements = pipeMdls.Where(pipe => pipe.IsPipesOuterDiameter);
//         // Получаем все существующие марки на активном виде
//         var existingAnnotations = GetExistingAnnotations(doc, activeView).Cast<IndependentTag>().ToList();
//         var pipeTagsInfo = GetPipeTags(existingAnnotations, activeView);
//         List<BoundingBoxXYZ> existingTagBounds = new List<BoundingBoxXYZ>();
//         var pipesSortered = elements.OrderBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).X)
//             .ThenBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).Y);
//
//         bool flag = false;
//         foreach (var pipe in pipesSortered)
//         {
//             if (pipe.Lenght.ToMillimeters() is > 500 and < 4000 && flag)
//             {
//                 flag = false;
//                 continue;
//             }
//
//             if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
//             {
//                 continue;
//             }
//
// //             //Получаем свободное пространство вокруг трубы
// //             // Определение центральной точки трубы
// //             XYZ midPoint = pipe.Curve.Evaluate(0.5, true);
// //             double searchRadius = UnitUtils.ConvertToInternalUnits(5000,UnitTypeId.Millimeters); // Радиус поиска в метрах
// //                 //  Создание сферы (если работаете в 3D) или прямоугольника (для 2D-вида)
// //                 XYZ minPoint = new XYZ(midPoint.X - searchRadius, midPoint.Y - searchRadius, midPoint.Z);
// //                 XYZ maxPoint = new XYZ(midPoint.X + searchRadius, midPoint.Y + searchRadius, midPoint.Z);
// //                 // Создание BoundingBoxXYZ для области поиска
// //                 BoundingBoxXYZ searchAreaBox = new BoundingBoxXYZ
// //                 {
// //                     Min = minPoint,
// //                     Max = maxPoint,
// //                     Transform = Transform.Identity
// //                 };
// //                 Outline outline = new Outline(searchAreaBox.Min, searchAreaBox.Max);
// //                 // Создание фильтра пересечения bounding box
// //                 BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(outline);
// // List<TagInfo> existingTags = new List<TagInfo>();
// // foreach (var existingAnnotation in existingAnnotations)
// // {
// //     existingTags.Add(new TagInfo(existingAnnotation as IndependentTag)
// //     {
// //         BoundingBox = existingAnnotation.get_BoundingBox(activeView)
// //     });
// // }    
// // // Сбор элементов, пересекающихся с прямоугольной областью
// //             // Определяем категории для труб
// //             var pipeCategories = new BuiltInCategory[] { BuiltInCategory.OST_PipeCurves };
// //
// //             // Определяем категории для аннотаций (можно добавить нужные категории)
// //             var annotationCategories = new[]
// //             {
// //                 BuiltInCategory.OST_GenericAnnotation, 
// //                 BuiltInCategory.OST_TextNotes,        
// //                 BuiltInCategory.OST_Tags,
// //                 BuiltInCategory.OST_PipeTags
// //                 
// //             };
// //
// //             // Создаем фильтр по категориям труб и аннотаций
// //             ElementMulticategoryFilter categoryFilter = new ElementMulticategoryFilter(pipeCategories.Concat(annotationCategories).ToList());
// //
// //             // Собираем элементы
// //             var collector = new FilteredElementCollector(doc)
// //                 .WherePasses(bboxFilter)
// //                 .WhereElementIsNotElementType()
// //                 .WherePasses(categoryFilter)
// //                 .Where(e => e.Id != pipe.Id); // Исключаем выбранную трубу
//
//
//             XYZ midPoint = pipe.Curve.Evaluate(0.5, true);
//             Reference pipeRef = new Reference(pipe.Pipe);
//             IndependentTag newTag =
//                 TryPlaceTagWithMultipleStrategies(doc, activeView, selectedTag, pipeRef, midPoint, existingTagBounds);
//
//             if (newTag != null)
//             {
//                 newTag.LeaderEndCondition = LeaderEndCondition.Free;
//                 newTag.TagHeadPosition = new XYZ(midPoint.X + 3, midPoint.Y + 2, midPoint.Z);
//                 existingTagBounds.Add(newTag.get_BoundingBox(activeView));
//             }
//
//             // //  Определяем область вокруг точки для проверки свободного пространства
//             //   double checkRadius = 0.5; // В метрах, измените по необходимости
//             //   BoundingBoxXYZ bbox = new BoundingBoxXYZ
//             //   {
//             //       Min = new XYZ(midPoint.X - checkRadius, midPoint.Y - checkRadius, 0),
//             //       Max = new XYZ(midPoint.X + checkRadius, midPoint.Y + checkRadius, 0)
//             //   };
//             //   Outline outline = new Outline(bbox.Min, bbox.Max);
//             //  // Создаем фильтр для элементов, пересекающих заданную область
//             //   BoundingBoxIntersectsFilter bboxFilter = new BoundingBoxIntersectsFilter(outline);
//             //  // Шаг 4: Вычисление позиций марки для текущей трубы
//             //   List<XYZ> tagLocations =
//             //       FindOptimalTagLocation(pipe, selectedTag, pipeTagsInfo);
//             //   var collector = new FilteredElementCollector(doc, activeView.Id)
//             //       .WherePasses(bboxFilter)
//             //       .WhereElementIsNotElementType();
//             //   if (tagLocations.Count == 0) continue;
//             //  // Создание марки
//             //       Reference reference = new Reference(pipe.Pipe);
//             //       if (tagLocation == null) continue;
//             //       IndependentTag pipeTag = IndependentTag.Create(doc, selectedTag.Id, activeView.Id,
//             //           reference, true, TagOrientation.Horizontal, tagLocation);
//             //       existingAnnotations.Add(pipeTag);
//             //       pipeTag.LeaderEndCondition = LeaderEndCondition.Free;
//             //       pipeTag.TagHeadPosition = new XYZ(tagLocation.X + 3, tagLocation.Y + 2, tagLocation.Z);
//             //       // pipeTag.SetLeaderElbow(reference,new XYZ(pipeTag.TagHeadPosition.X, pipeTag.TagHeadPosition.Y,pipeTag.TagHeadPosition.Z));
//             //   
//             flag = true;
//         }
//     }

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

    // public void PlacementMarksPipeInsulation(Document doc, List<PipeMdl> pipeMdls, View activeView,
    //     FamilySymbol selectedTag)
    // {
    //     var elements = pipeMdls.Where(pipe => pipe.IsInsulation);
    //     // Получаем все существующие марки на активном виде
    //     var existingAnnotations = GetExistingAnnotations(doc, activeView).Cast<IndependentTag>().ToList();
    //     var pipeTagsInfo = GetPipeTags(existingAnnotations, activeView);
    //
    //     var pipesSortered = elements.OrderBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).X)
    //         .ThenBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).Y);
    //
    //     bool flag = false;
    //     foreach (var pipe in pipesSortered)
    //     {
    //         if (pipe.Lenght.ToMillimeters() is > 500 and < 4000 && flag)
    //         {
    //             flag = false;
    //             continue;
    //         }
    //
    //         if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
    //         {
    //             continue;
    //         }
    //
    //         // Шаг 4: Вычисление позиций марки для текущей трубы
    //         List<XYZ> tagLocations =
    //             FindOptimalTagLocation(pipe, selectedTag, pipeTagsInfo);
    //         if (tagLocations.Count == 0) continue;
    //         // Создание марки
    //         foreach (var tagLocation in tagLocations)
    //         {
    //             if (tagLocation == null) continue;
    //             IndependentTag pipeTag = IndependentTag.Create(doc, selectedTag.Id, activeView.Id,
    //                 new Reference(pipe.Pipe), false, TagOrientation.Horizontal, tagLocation);
    //             existingAnnotations.Add(pipeTag);
    //         }
    //
    //         flag = true;
    //     }
    // }
    public void PlacementMarksSystemAbbreviation(List<PipeWrp> pipesWrp, List<TagWrp> tagWpr,
        FamilySymbol selectedTag)
    {
        const double TOL = 1000; //
        var activeView = _doc.ActiveView;
        var pipes = pipesWrp.Where(x => x.Length.ToMillimeters() > 1000).ToList();
        if (pipes.Count == 0) return;
        var existingSelectedTags = tagWpr
            .Where(x => x.TagTypeId == selectedTag.Id)
            .ToList();
        var pipeNotTags = GetPipeNotTags(pipes, existingSelectedTags);
        // Групировка по направлению
        var pipeGroupByDirection = pipeNotTags
            .GroupBy(p => p.Direction, new DirectionEqualityComparer())
            .ToList();
        // Дополнительная группировка труб, идущих друг за другом
        var finalGroupedPipes = new List<List<PipeWrp>>();

        foreach (var directionGroup in pipeGroupByDirection)
        {
            var sequentialGroups = GroupSequentialPipes(directionGroup.ToList());
            finalGroupedPipes.AddRange(sequentialGroups);
        }

        foreach (var pipeGroup in pipeGroupByDirection)
        {
            foreach (var pipe in pipeGroup)
            {
                if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
                {
                    continue;
                }

                // Вычисляем оптимальные позиции для марок
                List<double> tagPositions = CalculateTagPositions(pipe);

                // Размещаем марки в вычисленных позициях
                foreach (double position in tagPositions)
                {
                    XYZ point = pipe.StartPoint + pipe.Direction * position;
                    XYZ tagPoint = CalculateTagPosition(point, pipe);
                    if (tagPoint != null)
                    {
                        IndependentTag pipeTag = IndependentTag.Create(_doc, selectedTag.Id, activeView.Id,
                            new Reference(pipe.Pipe), false, TagOrientation.Horizontal, tagPoint);

                        var newPosition = FindFreeTagPosition(tagPoint, new TagWrp(pipeTag), pipe);
                        pipeTag.TagHeadPosition = newPosition;
                        // Проверяем, лежит ли марка на трубе
                        if (IsTagOnPipe(newPosition, pipe))
                        {
                            tagWpr.Add(new TagWrp(pipeTag));
                        }
                        else
                        {
                            _doc.Delete(pipeTag.Id);
                        }
                    }
                }
            }
        }
    }

    public List<List<PipeWrp>> GroupSequentialPipes(List<PipeWrp> pipes)
{
    const double proximityThreshold = 1000; // Пороговое расстояние для группировки

    var groupedPipes = new List<List<PipeWrp>>();

    // Сортируем трубы по позиции (например, по Y-координате)
    pipes = pipes.OrderBy(p => p.StartPoint.Y).ToList();

    List<PipeWrp> currentGroup = new List<PipeWrp>();

    for (int i = 0; i < pipes.Count; i++)
    {
        // Добавляем первую трубу в текущую группу
        if (currentGroup.Count == 0)
        {
            currentGroup.Add(pipes[i]);
        }
        else
        {
            // Проверяем, находится ли текущая труба в пределах порогового расстояния
            var lastPipeInGroup = currentGroup.Last();
            var distance = CalculateDistance(lastPipeInGroup, pipes[i]);

            if (distance < proximityThreshold.FromMillimeters() && AreAligned(lastPipeInGroup, pipes[i]))
            {
                currentGroup.Add(pipes[i]);
            }
            else
            {
                // Если трубы не достаточно близко, сохраняем текущую группу и начинаем новую
                groupedPipes.Add(currentGroup);
                currentGroup = new List<PipeWrp> { pipes[i] };
            }
        }
    }

    // Добавляем последнюю группу, если она содержит трубы
    if (currentGroup.Count > 0)
    {
        groupedPipes.Add(currentGroup);
    }

    return groupedPipes;
}

// Вспомогательный метод для расчета расстояния между трубами
private double CalculateDistance(PipeWrp pipe1, PipeWrp pipe2)
{
    // Расчет расстояния между концом первой трубы и началом второй
    return Math.Sqrt(
        Math.Pow(pipe2.StartPoint.X - pipe1.EndPoint.X, 2) +
        Math.Pow(pipe2.StartPoint.Y - pipe1.EndPoint.Y, 2)
    );
}

// Проверка выравнивания труб
private bool AreAligned(PipeWrp pipe1, PipeWrp pipe2)
{
    const double alignmentTolerance = 50; // Допуск для выравнивания

    // Проверяем, идут ли трубы в одном направлении
    return Math.Abs(pipe1.Direction.X - pipe2.Direction.X) < alignmentTolerance &&
           Math.Abs(pipe1.Direction.Y - pipe2.Direction.Y) < alignmentTolerance;
}
    
    /// <summary>Абсолютно-нормализованный вектор (всегда «положительный»).</summary>
    private static XYZ NormalizePositive(XYZ v)
    {
        v = v.Normalize();
        // «Разворачиваем» так, чтобы хотя бы первая ненулевая координата была >0
        if (v.X < 0 ||
            (Math.Abs(v.X) < 1e-9 && v.Y < 0) ||
            (Math.Abs(v.X) < 1e-9 && Math.Abs(v.Y) < 1e-9 && v.Z < 0))
        {
            v = v.Negate();
        }
        return v;
    }

    /// <summary>Возвращает трубу с ориентацией приведённой к общей оси.</summary>
    private static (PipeWrp pipe, XYZ start, XYZ end, double startT) OrientPipe(PipeWrp p, XYZ axis)
    {
        XYZ dir = p.Direction.Normalize();
        XYZ s   = p.StartPoint;
        XYZ e   = p.EndPoint;

        // Если труба «смотрит» в обратную сторону – разворачиваем
        if (dir.DotProduct(axis) < 0)
        {
            s = p.EndPoint;
            e = p.StartPoint;
        }

        // Скалярная координата вдоль оси – удобно для сортировки
        double t = s.DotProduct(axis);

        return (p, s, e, t);
    }
    /// <summary>
    /// Проверяет, находится ли марка на трубе
    /// </summary>
    /// <param name="tagPosition">Позиция марки</param>
    /// <param name="pipe">Труба</param>
    /// <returns>True, если марка находится на трубе, иначе False</returns>
    private bool IsTagOnPipe(XYZ tagPosition, PipeWrp pipe)
    {
        // Получаем начало и конец трубы
        XYZ startPoint = pipe.StartPoint;
        XYZ endPoint = pipe.EndPoint;

        // Проверяем, находится ли позиция марки между началом и концом трубы
        double tolerance = 0.01; // Допуск для проверки (например, 1 см)

        // Проверяем по X и Y координатам
        bool isOnPipe = (tagPosition.X >= Math.Min(startPoint.X, endPoint.X) - tolerance &&
                         tagPosition.X <= Math.Max(startPoint.X, endPoint.X) + tolerance &&
                         tagPosition.Y >= Math.Min(startPoint.Y, endPoint.Y) - tolerance &&
                         tagPosition.Y <= Math.Max(startPoint.Y, endPoint.Y) + tolerance);

        return isOnPipe;
    }

    /// <summary>
    /// Вычисляет оптимальные позиции для размещения марок на трубе
    /// </summary>
    /// <param name="pipe">Труба</param>
    /// <returns>Список позиций вдоль трубы (в внутренних единицах)</returns>
    private List<double> CalculateTagPositions(PipeWrp pipe)
    {
        List<double> positions = new List<double>();

        // Параметры
        double intervalMm = 3000; // мм
        double minDistanceFromEndsMm = 200; // мм

        // Конвертация в внутренние единицы
        double interval = UnitUtils.ConvertToInternalUnits(intervalMm, UnitTypeId.Millimeters);
        double minDistanceFromEnds = UnitUtils.ConvertToInternalUnits(minDistanceFromEndsMm, UnitTypeId.Millimeters);

        double pipeLengthMm = pipe.Length.ToMillimeters();

        if (pipeLengthMm <= intervalMm)
        {
            // Для коротких труб - одна марка в центре
            positions.Add(pipe.Length / 2);
        }
        else
        {
            // Вычисляем количество марок
            int numberOfTags = (int)Math.Ceiling(pipeLengthMm / intervalMm);

            // Пример: труба 6000мм, интервал 3000мм -> 2 марки
            // Позиции: 3000мм и 6000мм, но 6000мм смещаем к 5800мм

            for (int i = 1; i <= numberOfTags; i++)
            {
                double idealPosition = i * interval;
                double actualPosition = idealPosition;

                // Корректируем позицию, если она слишком близко к концу
                if (idealPosition > (pipe.Length - minDistanceFromEnds))
                {
                    actualPosition = pipe.Length - minDistanceFromEnds;
                }

                // Корректируем позицию, если она слишком близко к началу
                if (actualPosition < minDistanceFromEnds)
                {
                    actualPosition = minDistanceFromEnds;
                }

                // Проверяем, что позиция не дублируется
                if (!positions.Any(p => Math.Abs(p - actualPosition) < 0.01)) // 0.01 фута ~ 3мм
                {
                    positions.Add(actualPosition);
                }
            }
        }

        return positions.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Альтернативный метод с равномерным распределением марок
    /// </summary>
    private List<double> CalculateTagPositionsUniform(PipeWrp pipe)
    {
        List<double> positions = new List<double>();

        double intervalMm = 3000; // мм
        double minDistanceFromEndsMm = 600; // мм

        double interval = UnitUtils.ConvertToInternalUnits(intervalMm, UnitTypeId.Millimeters);
        double minDistanceFromEnds = UnitUtils.ConvertToInternalUnits(minDistanceFromEndsMm, UnitTypeId.Millimeters);

        double pipeLengthMm = pipe.Length.ToMillimeters();

        if (pipeLengthMm <= intervalMm)
        {
            // Короткая труба - марка в центре
            positions.Add(pipe.Length / 2);
        }
        else
        {
            // Вычисляем количество марок
            int numberOfTags = (int)Math.Ceiling(pipeLengthMm / intervalMm);

            // Доступная длина для размещения марок
            double availableLength = pipe.Length - (2 * minDistanceFromEnds);

            if (availableLength > 0 && numberOfTags > 1)
            {
                // Равномерно распределяем марки в доступной области
                double spacing = availableLength / (numberOfTags - 1);

                for (int i = 0; i < numberOfTags; i++)
                {
                    double position = minDistanceFromEnds + (i * spacing);
                    positions.Add(position);
                }
            }
            else
            {
                // Если доступной длины недостаточно, размещаем марки по интервалам со смещением
                for (int i = 1; i <= numberOfTags; i++)
                {
                    double position = i * interval;

                    // Смещаем от конца, если нужно
                    if (position > (pipe.Length - minDistanceFromEnds))
                    {
                        position = pipe.Length - minDistanceFromEnds;
                    }

                    positions.Add(position);
                }
            }
        }

        return positions.Distinct().OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Корректирует расстояние, чтобы марка не попадала слишком близко к концам трубы
    /// </summary>
    /// <param name="originalDistance">Исходное расстояние</param>
    /// <param name="pipeLength">Длина трубы</param>
    /// <param name="minDistanceFromEnds">Минимальное расстояние от концов</param>
    /// <returns>Скорректированное расстояние</returns>
    private double AdjustDistanceFromEnds(double originalDistance, double pipeLength, double minDistanceFromEnds)
    {
        // Если марка слишком близко к началу трубы
        if (originalDistance < minDistanceFromEnds)
        {
            return minDistanceFromEnds;
        }

        // Если марка слишком близко к концу трубы
        if (originalDistance > (pipeLength - minDistanceFromEnds))
        {
            return pipeLength - minDistanceFromEnds;
        }

        return originalDistance;
    }

    /// <summary>
    /// Улучшенный метод размещения марок с избеганием концов труб
    /// </summary>
    public void PlacementMarksSystemAbbreviationImproved(List<PipeWrp> pipeWrp, List<TagWrp> tagWpr,
        FamilySymbol selectedTag)
    {
        var existingSelectedTags = tagWpr
            .Where(x => x.TagTypeId == selectedTag.Id)
            .ToList();
        var pipes = GetPipeNotTags(pipeWrp, existingSelectedTags);
        var pipesSort = pipes.OrderBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).X)
            .ThenBy(p => ((LocationCurve)p.Pipe.Location).Curve.GetEndPoint(1).Y).ToList();
        bool flag = false;
        var activeView = _doc.ActiveView;

        // Параметры размещения марок
        double interval = 3000; // мм
        double minDistanceFromEnds = 200; // мм
        double minPipeLength = 400; // мм - минимальная длина трубы для размещения марки

        // Конвертация в внутренние единицы
        double intervalInternal = UnitUtils.ConvertToInternalUnits(interval, UnitTypeId.Millimeters);
        double minDistanceFromEndsInternal =
            UnitUtils.ConvertToInternalUnits(minDistanceFromEnds, UnitTypeId.Millimeters);
        double minPipeLengthInternal = UnitUtils.ConvertToInternalUnits(minPipeLength, UnitTypeId.Millimeters);

        foreach (var pipe in pipesSort)
        {
            if (pipe.Length.ToMillimeters() is > 500 and < 4000 && flag)
            {
                flag = false;
                continue;
            }

            if (activeView.ViewType == ViewType.FloorPlan && pipe.IsRiser)
            {
                continue;
            }

            // Пропускаем слишком короткие трубы
            if (pipe.Length < minPipeLengthInternal)
            {
                continue;
            }

            // Вычисляем оптимальные позиции для марок
            List<double> tagPositions =
                CalculateOptimalTagPositions(pipe, intervalInternal, minDistanceFromEndsInternal);

            // Размещаем марки в вычисленных позициях
            foreach (double position in tagPositions)
            {
                XYZ point = pipe.StartPoint + pipe.Direction * position;
                XYZ tagPoint = CalculateTagPosition(point, pipe);

                if (tagPoint != null)
                {
                    IndependentTag pipeTag = IndependentTag.Create(_doc, selectedTag.Id, activeView.Id,
                        new Reference(pipe.Pipe), false, TagOrientation.Horizontal, tagPoint);

                    var newPosition = FindFreeTagPosition(tagPoint, new TagWrp(pipeTag), pipe);
                    pipeTag.TagHeadPosition = newPosition;
                    tagWpr.Add(new TagWrp(pipeTag));
                }
            }

            flag = true;
        }
    }

    /// <summary>
    /// Вычисляет позицию марки с учетом смещения
    /// </summary>
    /// <param name="point">Точка на трубе</param>
    /// <param name="pipe">Труба</param>
    /// <returns>Позиция марки</returns>
    private XYZ CalculateTagPosition(XYZ point, PipeWrp pipe)
    {
        if (pipe.DisplacedPoint != null)
        {
            return new XYZ(
                point.X + pipe.DisplacedPoint.X,
                point.Y + pipe.DisplacedPoint.Y,
                point.Z + pipe.DisplacedPoint.Z);
        }
        else
        {
            return point;
        }
    }

    /// <summary>
    /// Вычисляет оптимальные позиции для размещения марок на трубе
    /// </summary>
    /// <param name="pipe">Труба</param>
    /// <param name="interval">Интервал между марками</param>
    /// <param name="minDistanceFromEnds">Минимальное расстояние от концов</param>
    /// <returns>Список позиций вдоль трубы</returns>
    private List<double> CalculateOptimalTagPositions(PipeWrp pipe, double interval, double minDistanceFromEnds)
    {
        List<double> positions = new List<double>();

        // Доступная длина для размещения марок
        double availableLength = pipe.Length - (2 * minDistanceFromEnds);

        if (availableLength <= 0)
        {
            // Если труба слишком короткая для отступов, размещаем марку в центре
            positions.Add(pipe.Length / 2);
            return positions;
        }

        if (pipe.Length <= interval)
        {
            // Для коротких труб - одна марка в центре
            positions.Add(pipe.Length / 2);
        }
        else
        {
            // Для длинных труб - несколько марок с равномерным распределением
            int numberOfTags = (int)(availableLength / interval);
            if (numberOfTags == 0) numberOfTags = 1;

            // Вычисляем равномерное распределение марок в доступной области
            double actualInterval = availableLength / numberOfTags;

            for (int i = 0; i < numberOfTags; i++)
            {
                double position = minDistanceFromEnds + (i + 0.5) * actualInterval;
                positions.Add(position);
            }
        }

        return positions;
    }

    /// <summary>
    /// Проверяет, не слишком ли близко марка к концам трубы
    /// </summary>
    /// <param name="position">Позиция вдоль трубы</param>
    /// <param name="pipeLength">Длина трубы</param>
    /// <param name="minDistance">Минимальное расстояние</param>
    /// <returns>True если позиция допустима</returns>
    private bool IsPositionValidFromEnds(double position, double pipeLength, double minDistance)
    {
        return position >= minDistance && position <= (pipeLength - minDistance);
    }


    private static List<PipeWrp> GetPipeNotTags(List<PipeWrp> pipesWrp, List<TagWrp> existingSelectedTags)
    {
        if (existingSelectedTags.Count == 0) return pipesWrp;

        // Получаем все ID элементов, которые помечены хотя бы одним тегом
        var taggedElementIds = existingSelectedTags
            .SelectMany(tag => tag.TaggedElements)
            .Select(element => element.Id.Value)
            .ToHashSet(); // Используем HashSet для быстрого поиска

        // Возвращаем только те трубы, которые НЕ помечены ни одним тегом
        return pipesWrp.Where(pipe => !taggedElementIds.Contains(pipe.Id.Value)).ToList();
    }

    public void TestTagBoundingBoxes(IndependentTag tag)
    {
        // Получить текущий документ и активный вид
        Document doc = Context.Document;
        View activeView = doc.ActiveView;
// Получаем BoundingBox в view coordinates
        BoundingBoxXYZ bbox = tag.get_BoundingBox(activeView);
        if (activeView.ViewType == ViewType.ThreeD)
        {
            Create3DBoxForVerification(doc, bbox);
        }

        if (activeView.ViewType == ViewType.FloorPlan)
        {
            Create2DBoxForVerification(doc, activeView, bbox);
        }
    }

    public void Create2DBoxForVerification(Document doc, View view, BoundingBoxXYZ bbox)
    {
        // Получаем размеры
        double width = bbox.Max.X - bbox.Min.X;
        double height = bbox.Max.Y - bbox.Min.Y;

        // Создаем линии, образующие прямоугольник
        Line line1 = Line.CreateBound(bbox.Min, new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Min.Z));
        Line line2 = Line.CreateBound(new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Min.Z), bbox.Max);
        Line line3 = Line.CreateBound(bbox.Max, new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Max.Z));
        Line line4 = Line.CreateBound(new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Max.Z), bbox.Min);

// Создаем DetailLine для каждой линии
        DetailCurve detailLine1 = doc.Create.NewDetailCurve(view, line1);
        DetailCurve detailLine2 = doc.Create.NewDetailCurve(view, line2);
        DetailCurve detailLine3 = doc.Create.NewDetailCurve(view, line3);
        DetailCurve detailLine4 = doc.Create.NewDetailCurve(view, line4);
    }

    public void Create3DBoxForVerification(Document doc, BoundingBoxXYZ boundingBox)
    {
        // Создаем геометрию бокса
        Solid boxSolid = CreateBoxSolidSimple(boundingBox);

        // Создаем DirectShape для отображения геометрии
        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));

        // Устанавливаем форму
        ds.SetShape(new GeometryObject[] { boxSolid });

        // Применяем прозрачность для лучшей видимости
        OverrideGraphicSettings ogs = new OverrideGraphicSettings();
        ogs.SetSurfaceTransparency(70);
        ogs.SetProjectionLineColor(new Color(255, 0, 0)); // Красный цвет

        // Применяем настройки отображения к элементу во всех видах
        foreach (View view in new FilteredElementCollector(doc).OfClass(typeof(View)))
        {
            doc.ActiveView.SetElementOverrides(ds.Id, ogs);
        }
    }

// Вспомогательный метод для создания Solid в форме бокса
    private Solid CreateBoxSolidSimple(BoundingBoxXYZ boundingBox)
    {
        if (boundingBox == null)
            return null;

        try
        {
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;

            // Нормализуем координаты
            double minX = Math.Min(min.X, max.X);
            double maxX = Math.Max(min.X, max.X);
            double minY = Math.Min(min.Y, max.Y);
            double maxY = Math.Max(min.Y, max.Y);
            double minZ = Math.Min(min.Z, max.Z);
            double maxZ = Math.Max(min.Z, max.Z);

            // Проверяем размеры
            const double tolerance = 1e-9;
            if (maxX - minX < tolerance || maxY - minY < tolerance || maxZ - minZ < tolerance)
            {
                return null; // Слишком маленький бокс
            }

            // Создаем прямоугольный профиль
            XYZ p1 = new XYZ(minX, minY, minZ);
            XYZ p2 = new XYZ(maxX, minY, minZ);
            XYZ p3 = new XYZ(maxX, maxY, minZ);
            XYZ p4 = new XYZ(minX, maxY, minZ);

            Line line1 = Line.CreateBound(p1, p2);
            Line line2 = Line.CreateBound(p2, p3);
            Line line3 = Line.CreateBound(p3, p4);
            Line line4 = Line.CreateBound(p4, p1);

            CurveLoop curveLoop = new CurveLoop();
            curveLoop.Append(line1);
            curveLoop.Append(line2);
            curveLoop.Append(line3);
            curveLoop.Append(line4);

            List<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };

            double extrusionHeight = maxZ - minZ;

            return GeometryCreationUtilities.CreateExtrusionGeometry(
                curveLoops,
                XYZ.BasisZ,
                extrusionHeight);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка создания простого Solid: {ex.Message}");
            return null;
        }
    }

    public BoundingBoxXYZ Get3DTagBoundingBox(IndependentTag tag, Document doc)
    {
        // Получаем BoundingBox тега в модельном пространстве
        BoundingBoxXYZ boundingBox = tag.get_BoundingBox(Context.ActiveView);

        // Если BoundingBox не существует, пробуем получить его через геометрию
        if (boundingBox == null)
        {
            Options options = new Options();
            options.ComputeReferences = true;
            options.DetailLevel = ViewDetailLevel.Fine;

            GeometryElement geomElem = tag.get_Geometry(options);
            if (geomElem != null)
            {
                // Создаем новый BoundingBox
                boundingBox = new BoundingBoxXYZ();
                boundingBox.Min = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                boundingBox.Max = new XYZ(double.MinValue, double.MinValue, double.MinValue);

                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is Solid solid)
                    {
                        BoundingBoxXYZ solidBox = solid.GetBoundingBox();
                        if (solidBox != null)
                        {
                            // Расширяем границы для включения этого объекта
                            boundingBox.Min = new XYZ(
                                Math.Min(boundingBox.Min.X, solidBox.Min.X),
                                Math.Min(boundingBox.Min.Y, solidBox.Min.Y),
                                Math.Min(boundingBox.Min.Z, solidBox.Min.Z));

                            boundingBox.Max = new XYZ(
                                Math.Max(boundingBox.Max.X, solidBox.Max.X),
                                Math.Max(boundingBox.Max.Y, solidBox.Max.Y),
                                Math.Max(boundingBox.Max.Z, solidBox.Max.Z));
                        }
                    }
                }
            }
        }

        return boundingBox;
    }


// private Dictionary<ElementId, List<TagWrapper>> GetPipeTags(List<TagWrapper> existingAnnotations)
// {
//     var pipeTagsInfo = new Dictionary<ElementId, List<TagWrapper>>();
//     foreach (var tagModel in existingAnnotations)
//     {
//         foreach (var taggedElement in tagModel.TaggedElements)
//         {
//           
//
//             // Получение кривой трубы и проекции положения тега на кривую
//             Curve pipeCurve = tagModel.Curve;
//            
//             IntersectionResult result = pipeCurve.Project(tagModel.TagHeadPosition);
//             double parameter = result.Parameter;
//
//             // Добавление записи для трубы, если её еще нет в словаре
//             if (!pipeTagsInfo.ContainsKey(pipe.Id))
//             {
//                 pipeTagsInfo[pipe.Id] = new List<TagWrapper>();
//             }
//
//             // Вычисление расстояния от начала трубы до точки проекции тега
//             double distanceAlongCurve = CalculateDistanceAlongPipe(pipeCurve, parameter);
//             tagModel.Distance = distanceAlongCurve;
//
//             pipeTagsInfo[pipe.Id].Add(tagModel);
//         }
//     }
//
//     return pipeTagsInfo;
// }


    /// Вычисление расстояния вдоль трубы от начала до параметра
    private double CalculateDistanceAlongPipe(Curve pipeCurve, double parameter)
    {
        double startParam = pipeCurve.GetEndParameter(0);
        double endParam = parameter;

        // Проверка порядка параметров
        if (startParam > endParam)
        {
            (startParam, endParam) = (endParam, startParam);
        }

        // Проверка минимального расстояния
        if (Math.Abs(startParam - endParam) < 0.001)
        {
            XYZ startPoint = pipeCurve.Evaluate(startParam, false);
            XYZ endPoint = pipeCurve.Evaluate(endParam, false);
            return startPoint.DistanceTo(endPoint);
        }

        // Вычисление длины части кривой
        try
        {
            Curve partialCurve = pipeCurve.Clone();
            partialCurve.MakeBound(startParam, endParam);
            return partialCurve.Length;
        }
        catch (Exception)
        {
            // В случае ошибки при создании ограниченной кривой
            return 0.0;
        }
    }

    /// <summary>
    /// Получает список существующих аннотаций в текущем представлении
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="view">Текущее представление</param>
    /// <returns>Список элементов аннотаций</returns>
    public IEnumerable<Element> GetExistingAnnotations(Document doc, View view)
    {
        // Получаем IndependentTags и TextNotes в текущем представлении
        return new FilteredElementCollector(doc, view.Id)
            .OfClass(typeof(IndependentTag))
            .WhereElementIsNotElementType();
    }


// Метод для поиска свободной позиции для марки
    /// <summary>
    /// Находит свободную позицию для марки, избегая пересечений с другими марками
    /// </summary>
    /// <param name="originalPosition">Исходная позиция марки</param>
    /// <param name="existingTag">Существующая марка</param>
    /// <param name="pipe">Труба, к которой относится марка</param>
    /// <returns>Новая позиция марки или исходная, если пересечений нет</returns>
    private XYZ FindFreeTagPosition(XYZ originalPosition, TagWrp existingTag, PipeWrp pipe)
    {
        // Максимальное количество попыток смещения для каждой позиции
        int maxAttempts = 10;

        // Получаем все пересекающиеся элементы
        List<IndependentTag> intersectingTags = TagIntersectionHelper.GetIntersectingTags(
            _doc, existingTag.IndependentTag, _doc.ActiveView);
        // Получаем пересекающиеся трубы (исключая текущую трубу)
        List<Pipe> intersectingPipes = TagIntersectionHelper.GetIntersectingPipes(existingTag, pipe, _doc.ActiveView);
        // Если нет пересечений, возвращаем исходную позицию
        // Если нет пересечений, возвращаем исходную позицию
        if ((intersectingTags == null || intersectingTags.Count == 0) &&
            (intersectingPipes == null || intersectingPipes.Count == 0))
            return originalPosition;

        // Направление смещения (параллельное направлению трубы в плоскости XY)
        XYZ shiftDirection = GetParallelDirectionXy(pipe);


        double baseShiftDistance = UnitUtils.ConvertToInternalUnits(400, UnitTypeId.Millimeters);

        // Пробуем различные стратегии смещения
        XYZ newPosition = TryShiftStrategies(originalPosition, existingTag, intersectingTags,
            intersectingPipes, shiftDirection, baseShiftDistance, maxAttempts);

        return newPosition ?? originalPosition;
    }

// <summary>
    /// Пробует различные стратегии смещения марки
    /// </summary>
    /// <param name="originalPosition">Исходная позиция</param>
    /// <param name="existingTag">Существующая марка</param>
    /// <param name="intersectingTags">Список пересекающихся марок</param>
    /// <param name="primaryDirection">Основное направление смещения</param>
    /// <param name="baseDistance">Базовое расстояние смещения</param>
    /// <param name="maxAttempts">Максимальное количество попыток</param>
    /// <returns>Новая позиция или null</returns>
    private XYZ TryShiftStrategies(XYZ originalPosition, TagWrp existingTag,
        List<IndependentTag> intersectingTags, List<Pipe> intersectingPipes,
        XYZ primaryDirection, double baseDistance, int maxAttempts)
    {
        // Стратегия 1: Смещение вдоль основного направления
        XYZ newPosition = TryShiftInDirection(originalPosition, existingTag, intersectingTags,
            intersectingPipes, primaryDirection, baseDistance, maxAttempts);
        if (newPosition != null) return newPosition;

        // Стратегия 2: Смещение в противоположном направлении
        newPosition = TryShiftInDirection(originalPosition, existingTag, intersectingTags,
            intersectingPipes, primaryDirection.Negate(), baseDistance, maxAttempts);
        if (newPosition != null) return newPosition;

        // Стратегия 3: Смещение перпендикулярно основному направлению
        XYZ perpendicularDirection = new XYZ(-primaryDirection.Y, primaryDirection.X, 0).Normalize();
        newPosition = TryShiftInDirection(originalPosition, existingTag, intersectingTags,
            intersectingPipes, perpendicularDirection, baseDistance, maxAttempts);
        if (newPosition != null) return newPosition;

        // Стратегия 4: Смещение в противоположном перпендикулярном направлении
        newPosition = TryShiftInDirection(originalPosition, existingTag, intersectingTags,
            intersectingPipes, perpendicularDirection.Negate(), baseDistance, maxAttempts);
        if (newPosition != null) return newPosition;

        // Стратегия 5: Радиальное смещение
        newPosition = TryRadialShift(originalPosition, existingTag, intersectingTags,
            intersectingPipes, baseDistance, maxAttempts);

        return newPosition;
    }


    /// <summary>
    /// Пробует смещение в заданном направлении
    /// </summary>
    /// <param name="originalPosition">Исходная позиция</param>
    /// <param name="existingTag">Существующая марка</param>
    /// <param name="intersectingTags">Список пересекающихся марок</param>
    /// <param name="direction">Направление смещения</param>
    /// <param name="baseDistance">Базовое расстояние</param>
    /// <param name="maxAttempts">Максимальное количество попыток</param>
    /// <returns>Новая позиция или null</returns>
    private XYZ TryShiftInDirection(XYZ originalPosition, TagWrp existingTag,
        List<IndependentTag> intersectingTags, List<Pipe> intersectingPipes,
        XYZ direction, double baseDistance, int maxAttempts)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            double distance = baseDistance * attempt;
            XYZ testPosition = originalPosition + direction * distance;

            if (IsPositionFree(testPosition, existingTag, intersectingTags, intersectingPipes))
            {
                return testPosition;
            }
        }

        return null;
    }

    /// <summary>
    /// Пробует радиальное смещение (по кругу вокруг исходной позиции)
    /// </summary>
    /// <param name="originalPosition">Исходная позиция</param>
    /// <param name="existingTag">Существующая марка</param>
    /// <param name="intersectingTags">Список пересекающихся марок</param>
    /// <param name="baseDistance">Базовое расстояние</param>
    /// <param name="maxAttempts">Максимальное количество попыток</param>
    /// <returns>Новая позиция или null</returns>
    private XYZ TryRadialShift(XYZ originalPosition, TagWrp existingTag,
        List<IndependentTag> intersectingTags, List<Pipe> intersectingPipes,
        double baseDistance, int maxAttempts)
    {
        int angleSteps = 8; // 8 направлений (каждые 45 градусов)

        for (int distanceStep = 1; distanceStep <= maxAttempts; distanceStep++)
        {
            double distance = baseDistance * distanceStep;

            for (int angleStep = 0; angleStep < angleSteps; angleStep++)
            {
                double angle = (2 * Math.PI * angleStep) / angleSteps;
                XYZ direction = new XYZ(Math.Cos(angle), Math.Sin(angle), 0);
                XYZ testPosition = originalPosition + direction * distance;

                if (IsPositionFree(testPosition, existingTag, intersectingTags, intersectingPipes))
                {
                    return testPosition;
                }
            }
        }

        return null;
    }

    private bool IsPositionFree(XYZ testPosition, TagWrp existingTag,
        List<IndependentTag> intersectingTags, List<Pipe> intersectingPipes)
    {
        try
        {
            // Создаем временный bounding box для тестовой позиции
            BoundingBoxXYZ testBoundingBox = CreateTestBoundingBox(testPosition, existingTag);
            if (testBoundingBox == null) return false;

            // Проверяем пересечение с марками
            if (intersectingTags != null)
            {
                foreach (var intersectingTag in intersectingTags)
                {
                    BoundingBoxXYZ otherBoundingBox = intersectingTag.get_BoundingBox(_doc.ActiveView);
                    if (otherBoundingBox != null && BoundingBoxesIntersect2D(testBoundingBox, otherBoundingBox))
                    {
                        return false;
                    }
                }
            }

            // Проверяем пересечение с трубами
            if (intersectingPipes != null)
            {
                foreach (var intersectingPipe in intersectingPipes)
                {
                    BoundingBoxXYZ pipeBoundingBox = intersectingPipe.get_BoundingBox(_doc.ActiveView);
                    if (pipeBoundingBox != null && BoundingBoxesIntersect2D(testBoundingBox, pipeBoundingBox))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Вычисляет центр масс списка марок
    /// </summary>
    /// <param name="tags">Список марок</param>
    /// <returns>Центр масс или null</returns>
    private XYZ CalculateTagsCentroid(List<IndependentTag> tags)
    {
        if (tags == null || tags.Count == 0) return null;

        double sumX = 0, sumY = 0, sumZ = 0;
        int validCount = 0;

        foreach (var tag in tags)
        {
            XYZ position = tag.TagHeadPosition;
            if (position != null)
            {
                sumX += position.X;
                sumY += position.Y;
                sumZ += position.Z;
                validCount++;
            }
        }

        if (validCount == 0) return null;

        return new XYZ(sumX / validCount, sumY / validCount, sumZ / validCount);
    }

    /// <summary>
    /// Проверяет, свободна ли позиция от пересечений
    /// </summary>
    /// <param name="testPosition">Тестируемая позиция</param>
    /// <param name="existingTag">Существующая марка</param>
    /// <param name="intersectingTags">Список пересекающихся марок</param>
    /// <returns>True если позиция свободна</returns>
    private bool IsPositionFree(XYZ testPosition, TagWrp existingTag, List<IndependentTag> intersectingTags)
    {
        try
        {
            // Создаем временный bounding box для тестовой позиции
            BoundingBoxXYZ testBoundingBox = CreateTestBoundingBox(testPosition, existingTag);
            if (testBoundingBox == null) return false;

            // Проверяем пересечение с каждой из пересекающихся марок
            foreach (var intersectingTag in intersectingTags)
            {
                BoundingBoxXYZ otherBoundingBox = intersectingTag.get_BoundingBox(_doc.ActiveView);
                if (otherBoundingBox != null && BoundingBoxesIntersect2D(testBoundingBox, otherBoundingBox))
                {
                    return false; // Есть пересечение
                }
            }

            return true; // Пересечений нет
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Создает тестовый bounding box для позиции
    /// </summary>
    /// <param name="position">Позиция</param>
    /// <param name="existingTag">Существующая марка для определения размеров</param>
    /// <returns>BoundingBox или null</returns>
    private BoundingBoxXYZ CreateTestBoundingBox(XYZ position, TagWrp existingTag)
    {
        try
        {
            // Получаем размеры существующей марки
            BoundingBoxXYZ existingBoundingBox = existingTag.IndependentTag.get_BoundingBox(_doc.ActiveView);

            double width = 1.0; // Значения по умолчанию
            double height = 0.5;

            if (existingBoundingBox != null)
            {
                width = Math.Abs(existingBoundingBox.Max.X - existingBoundingBox.Min.X);
                height = Math.Abs(existingBoundingBox.Max.Y - existingBoundingBox.Min.Y);
            }

            BoundingBoxXYZ testBoundingBox = new BoundingBoxXYZ();
            testBoundingBox.Min = new XYZ(
                position.X - width / 2,
                position.Y - height / 2,
                position.Z - 0.1);
            testBoundingBox.Max = new XYZ(
                position.X + width / 2,
                position.Y + height / 2,
                position.Z + 0.1);

            return testBoundingBox;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Проверяет пересечение двух bounding box в 2D
    /// </summary>
    /// <param name="box1">Первый bounding box</param>
    /// <param name="box2">Второй bounding box</param>
    /// <returns>True если пересекаются</returns>
    private bool BoundingBoxesIntersect2D(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        if (box1 == null || box2 == null) return false;

        // Проверяем пересечение по X и Y
        bool xIntersects = box1.Min.X <= box2.Max.X && box1.Max.X >= box2.Min.X;
        bool yIntersects = box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y;

        return xIntersects && yIntersects;
    }

    /// <summary>
    /// Применяет найденную позицию к марке
    /// </summary>
    /// <param name="tag">Марка</param>
    /// <param name="newPosition">Новая позиция</param>
    /// <returns>True если успешно</returns>
    private bool ApplyNewTagPosition(TagWrp tag, XYZ newPosition)
    {
        try
        {
            // Перемещаем марку
            tag.IndependentTag.TagHeadPosition = newPosition;


            return true;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", $"Не удалось переместить марку: {ex.Message}");
            return false;
        }
    }

    private bool IsPositionWithinPipeBounds(XYZ position, PipeWrp pipe, double exclusionTolerance)
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
    private bool IsPositionOccupied(XYZ position, TagWrp existingTag, PipeWrp pipe)
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

    private XYZ GetParallelDirectionXy(PipeWrp pipe)
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