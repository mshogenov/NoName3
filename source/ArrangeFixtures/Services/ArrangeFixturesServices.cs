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

            // Вычисляем шаг в футах (500 мм)
            double stepInFeet = UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters);

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
                    if (IsPointOnPipe(supportPoint, pipeStart, pipeEnd))
                    {
                        var level = _doc.GetElement(pipe.LevelId);
                        FamilyInstance family = _doc.Create.NewFamilyInstance(
                            supportPoint,
                            symbol,
                            level,
                            Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                        AlignInstanceWithPipe(family, pipe);
                    }
                }
            }

            trans.Commit();
        }
        catch (Exception e)
        {
            trans.RollBack();
        }
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