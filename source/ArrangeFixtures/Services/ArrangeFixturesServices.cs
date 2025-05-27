using ArrangeFixtures.Models;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Extensions;

namespace ArrangeFixtures.Services;

public class ArrangeFixturesServices
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private readonly ActionEventHandler _actionEventHandler = new();

    public void ArrangeFixtures(List<Pipe> pipes, Element selectedFixture)
    {
        if (!pipes.Any(x => x.IsValidObject))
        {
            return;
        }

        FamilySymbol symbol = null;
        // Определяем символ семейства
        if (selectedFixture is FamilySymbol fs)
        {
            symbol = fs;
        }
        else if (selectedFixture is FamilyInstance instance)
        {
            symbol = instance.Symbol;
        }

        if (symbol == null)
            return;

        // Активируем символ семейства, если он еще не активирован
        if (!symbol.IsActive)
            symbol.Activate();


        using Transaction trans = new Transaction(_doc, "Размещение креплений");
        try
        {
            trans.Start();

            // Определяем общее направление для всех труб
            XYZ commonDirection = null;
            XYZ referencePoint = null;

            // Находим общее направление и опорную точку
            foreach (var pipe in pipes.Where(x => x.IsValidObject))
            {
                if (pipe == null) return;
                var pipeLine = (pipe.Location as LocationCurve)?.Curve as Line;
                if (pipeLine == null) continue;

                XYZ pipeStart = pipeLine.GetEndPoint(0);
                XYZ pipeDirection = pipeLine.Direction.Normalize();

                if (commonDirection == null)
                {
                    commonDirection = pipeDirection;
                    referencePoint = pipeStart;
                }
                else
                {
                    // Проверяем, параллельны ли трубы
                    if (Math.Abs(commonDirection.DotProduct(pipeDirection)) < 0.99)
                    {
                        // Трубы не параллельны, можно обрабатывать их отдельно
                        continue;
                    }

                    // Убедимся, что направления совпадают (не противоположны)
                    if (commonDirection.DotProduct(pipeDirection) < 0)
                    {
                        pipeDirection = -pipeDirection;
                    }
                }
            }

            if (commonDirection == null)
                return; // Нет труб для обработки

            // Вычисляем шаг в футах (1000 мм)
            double stepInFeet = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);

            // Определяем максимальную длину среди всех труб
            double maxLength = 0;
            foreach (var pipe in pipes)
            {
                var pipeLine = (pipe.Location as LocationCurve)?.Curve as Line;
                if (pipeLine == null) continue;

                double length = pipeLine.Length;
                if (length > maxLength)
                    maxLength = length;
            }

            // Вычисляем количество креплений
            int supportCount = (int)Math.Floor(maxLength / stepInFeet) + 1;

            // Для каждой позиции крепления
            for (int i = 0; i < supportCount; i++)
            {
                double offset = i * stepInFeet;

                // Для каждой трубы устанавливаем крепление на этой позиции
                foreach (var pipe in pipes)
                {
                    var pipeLine = (pipe.Location as LocationCurve)?.Curve as Line;
                    if (pipeLine == null) continue;

                    XYZ pipeStart = pipeLine.GetEndPoint(0);
                    XYZ pipeEnd = pipeLine.GetEndPoint(1);
                    XYZ pipeDirection = pipeLine.Direction.Normalize();

                    // Убедимся, что направления совпадают с общим направлением
                    if (commonDirection.DotProduct(pipeDirection) < 0)
                    {
                        pipeDirection = -pipeDirection;
                        // Меняем местами начало и конец
                        (pipeStart, pipeEnd) = (pipeEnd, pipeStart);
                    }

                    // Проецируем начальную точку трубы на общую ось
                    XYZ projectedStart = ProjectPointToAxis(pipeStart, referencePoint, commonDirection);

                    // Вычисляем смещение начальной точки трубы от опорной точки вдоль общей оси
                    double startOffset = projectedStart.DistanceTo(referencePoint);
                    if (projectedStart.Subtract(referencePoint).DotProduct(commonDirection) < 0)
                        startOffset = -startOffset;

                    // Вычисляем позицию крепления с учетом смещения начальной точки
                    XYZ supportPoint = pipeStart + pipeDirection * (offset - startOffset);

                    // Проверяем, находится ли точка на трубе
                    if (!IsPointOnPipe(supportPoint, pipeStart, pipeEnd)) continue;
                    var level = _doc.GetElement(pipe.LevelId);
                    FamilyInstance family = _doc.Create.NewFamilyInstance(
                        supportPoint,
                        symbol,
                        level,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    AlignInstanceWithPipe(family, pipe);
                }
            }

            trans.Commit();
        }
        catch (Exception e)
        {
            trans.RollBack();
        }
    }
    public PipeExtremums FindExtremePipes(Document doc)
    {
        var collector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_PipeCurves)
            .OfClass(typeof(Pipe))
            .Cast<Pipe>();

        var result = new PipeExtremums();
        double maxX = double.MinValue;
        double minX = double.MaxValue;
        double maxY = double.MinValue;
        double minY = double.MaxValue;
        double maxZ = double.MinValue;
        double minZ = double.MaxValue;

        foreach (Pipe pipe in collector)
        {
            if (pipe == null)
            {
                
            }
            LocationCurve location = pipe.Location as LocationCurve;
            Curve curve = location.Curve;
            XYZ start = curve.GetEndPoint(0);
            XYZ end = curve.GetEndPoint(1);

            // Проверяем X координату
            if (start.X > maxX || end.X > maxX)
            {
                maxX = Math.Max(start.X, end.X);
                result.MaxX = pipe;
            }
            if (start.X < minX || end.X < minX)
            {
                minX = Math.Min(start.X, end.X);
                result.MinX = pipe;
            }

            // Проверяем Y координату
            if (start.Y > maxY || end.Y > maxY)
            {
                maxY = Math.Max(start.Y, end.Y);
                result.MaxY = pipe;
            }
            if (start.Y < minY || end.Y < minY)
            {
                minY = Math.Min(start.Y, end.Y);
                result.MinY = pipe;
            }

            // Проверяем Z координату
            if (start.Z > maxZ || end.Z > maxZ)
            {
                maxZ = Math.Max(start.Z, end.Z);
                result.MaxZ = pipe;
            }
            if (start.Z < minZ || end.Z < minZ)
            {
                minZ = Math.Min(start.Z, end.Z);
                result.MinZ = pipe;
            }
        }

        return result;
    }
public IList<(XYZ Point, Pipe NearbyPipe)> CheckPipePoints(Pipe sourcePipe, double spacing = 500, double searchRange = 1000)
{
    Document doc = sourcePipe.Document;
    var results = new List<(XYZ Point, Pipe NearbyPipe)>();

    // Получаем линию трубы
    LocationCurve locationCurve = sourcePipe.Location as LocationCurve;
    Curve pipeCurve = locationCurve.Curve;

    // Получаем длину трубы
    double pipeLength = pipeCurve.Length;

    // Собираем все трубы в проекте (кроме исходной)
    var allPipes = new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_PipeCurves)
        .OfClass(typeof(Pipe))
        .Where(p => p.Id != sourcePipe.Id)
        .Cast<Pipe>()
        .ToList();

    // Проверяем точки через каждые spacing миллиметров
    for (double currentDistance = 0; currentDistance <= pipeLength; currentDistance += spacing)
    {
        // Получаем точку на трубе
        XYZ point = pipeCurve.Evaluate(currentDistance / pipeLength, true);

        // Проецируем точку на горизонтальную плоскость
        XYZ projectedPoint = new XYZ(point.X, point.Y, 0);

        // Ищем ближайшую трубу к этой точке
        Pipe nearestPipe = null;
        double minDistance = double.MaxValue;

        foreach (Pipe pipe in allPipes)
        {
            LocationCurve pipeLocCurve = pipe.Location as LocationCurve;
            Curve pCurve = pipeLocCurve.Curve;

            // Проецируем трубу на горизонтальную плоскость
            XYZ pStart = pCurve.GetEndPoint(0);
            XYZ pEnd = pCurve.GetEndPoint(1);
            XYZ pStartProj = new XYZ(pStart.X, pStart.Y, 0);
            XYZ pEndProj = new XYZ(pEnd.X, pEnd.Y, 0);
            Line pProjectedLine = Line.CreateBound(pStartProj, pEndProj);

            // Находим расстояние от точки до линии
            double distance = GetDistanceFromPointToLine(projectedPoint, pProjectedLine);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPipe = pipe;
            }
        }

        // Если нашли трубу в пределах указанного диапазона
        if (minDistance <= searchRange)
        {
            results.Add((point, nearestPipe));
        }
    }

    return results;
}

private double GetDistanceFromPointToLine(XYZ point, Line line)
{
    XYZ start = line.GetEndPoint(0);
    XYZ end = line.GetEndPoint(1);
    XYZ vector = end - start;

    double lineLength = vector.GetLength();
    vector = vector.Normalize();

    XYZ pointVector = point - start;
    double dot = pointVector.DotProduct(vector);

    if (dot <= 0)
        return point.DistanceTo(start);
    if (dot >= lineLength)
        return point.DistanceTo(end);

    XYZ projection = start + vector * dot;
    return point.DistanceTo(projection);
}
    // Проецирует точку на ось, заданную опорной точкой и направлением
    private XYZ ProjectPointToAxis(XYZ point, XYZ axisPoint, XYZ axisDirection)
    {
        XYZ vector = point - axisPoint;
        double projection = vector.DotProduct(axisDirection);
        return axisPoint + axisDirection * projection;
    }

// Проверяет, находится ли точка на отрезке трубы
    private bool IsPointOnPipe(XYZ point, XYZ pipeStart, XYZ pipeEnd)
    {
        double pipeLength = pipeStart.DistanceTo(pipeEnd);
        double startDist = pipeStart.DistanceTo(point);
        double endDist = pipeEnd.DistanceTo(point);

        // Допустимая погрешность
        const double tolerance = 0.001;

        return Math.Abs(startDist + endDist - pipeLength) < tolerance &&
               startDist <= pipeLength + tolerance &&
               endDist <= pipeLength + tolerance;
    }

    /// <summary>
    /// Выравнивает экземпляр семейства с трубой
    /// </summary>
    /// <summary>
    /// Выравнивает экземпляр семейства с трубой, используя коннекторы
    /// </summary>
    private void AlignInstanceWithPipe(FamilyInstance instance, Pipe pipe)
    {
        // Получаем коннекторы трубы
        ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
        if (pipeConnectors.Size == 0)
            return;

        // Получаем коннекторы семейства
        ConnectorSet instanceConnectors = instance.MEPModel?.ConnectorManager?.Connectors;
        if (instanceConnectors == null || instanceConnectors.Size == 0)
            return;

        // Находим направление трубы
        XYZ pipeDirection = pipe.GetPipeDirection();

        // Находим локацию семейства
        LocationPoint locationPoint = instance.Location as LocationPoint;
        if (locationPoint == null)
            return;

        XYZ instancePoint = locationPoint.Point;

        // Находим основной коннектор семейства (например, первый)
        Connector primaryInstanceConnector = null;
        foreach (Connector conn in instanceConnectors)
        {
            primaryInstanceConnector = conn;
            break; // Берем первый коннектор
        }

        if (primaryInstanceConnector == null)
            return;

        // Получаем направление коннектора семейства
        XYZ instanceDirection = primaryInstanceConnector.CoordinateSystem.BasisZ;

        // Вычисляем угол между направлениями
        double angle = instanceDirection.AngleTo(pipeDirection);

        // Определяем ось вращения
        XYZ rotationAxis = instanceDirection.CrossProduct(pipeDirection);

        // Проверяем, не параллельны ли векторы
        if (rotationAxis.IsZeroLength())
        {
            // Если векторы параллельны, проверяем, нужно ли развернуть на 180 градусов
            if (instanceDirection.DotProduct(pipeDirection) < 0)
            {
                // Векторы направлены противоположно, используем перпендикулярную ось для поворота на 180°
                rotationAxis = XYZ.BasisX.CrossProduct(instanceDirection);
                if (rotationAxis.IsZeroLength())
                    rotationAxis = XYZ.BasisY.CrossProduct(instanceDirection);

                angle = Math.PI; // 180 градусов
            }
            else
            {
                // Векторы уже сонаправлены
                return;
            }
        }

        // Создаем ось для вращения
        Line axis = Line.CreateBound(instancePoint, instancePoint + rotationAxis);

        // Поворачиваем экземпляр
        ElementTransformUtils.RotateElement(_doc, instance.Id, axis, angle);
    }
}