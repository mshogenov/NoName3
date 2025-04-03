using Autodesk.Revit.DB.Plumbing;
using NumberingOfRisers.Models;

namespace NumberingOfRisers.Services;

public class RiserNumberingService
{
    // Направления нумерации по осям X и Y
    private readonly NumberingDirection _xDirection;
    private readonly NumberingDirection _yDirection;
    private readonly SortDirection _primarySortDirection;

    public RiserNumberingService(SortDirection primarySortDirection,
        NumberingDirection xDir = NumberingDirection.LeftToRight,
        NumberingDirection yDir = NumberingDirection.BottomToTop)
    {
        _primarySortDirection = primarySortDirection;
        _xDirection = xDir;
        _yDirection = yDir;
        _primarySortDirection = primarySortDirection;
    }

    public List<Riser> NumberRisers(List<Riser> riserGroups)
    {
        // Получаем центры всех групп стояков
        var riserCenters = new Dictionary<Riser, XYZ>();

        foreach (var riser in riserGroups)
        {
            // Вычисляем среднюю точку для каждой группы стояков
            var centerPoint = GetRiserCenter(riser.Pipes);
            riserCenters[riser] = centerPoint;
        }

        // Сортируем стояки согласно настройкам направления
        var sortedRisers = SortRisersByLocation(riserGroups, riserCenters);
        int number = 1;
        foreach (var riser in sortedRisers)
        {
            foreach (var pipe in riser.Pipes)
            {
                Parameter parameter = pipe.FindParameter("ADSK_Номер стояка");
                if (parameter != null && parameter.AsValueString() != number.ToString())
                {
                    parameter.Set(number.ToString());
                }
            }

            number++;
        }


        return sortedRisers;
    }

    private XYZ GetRiserCenter(List<Pipe> pipes)
    {
        // Вычисляем центр группы труб, усредняя их координаты
        double sumX = 0, sumY = 0;
        foreach (var pipe in pipes)
        {
            if (pipe.Location is LocationCurve locationCurve)
            {
                var point = locationCurve.Curve.GetEndPoint(0);
                sumX += point.X;
                sumY += point.Y;
            }
        }

        int count = pipes.Count;
        return new XYZ(sumX / count, sumY / count, 0); // Z не важна для нумерации
    }

    private List<Riser> SortRisersByLocation(List<Riser> risers,
        Dictionary<Riser, XYZ> centers)
    {
        // Создаем уникальные идентификаторы для стояков
        Dictionary<Riser, Guid> riserIds = new Dictionary<Riser, Guid>();
        foreach (var riser in risers)
        {
            riserIds[riser] = Guid.NewGuid();
        }

        List<Riser> result;
        if (_primarySortDirection == SortDirection.X)
        {
            // Сначала сортируем по X, затем по Y
            if (_xDirection == NumberingDirection.LeftToRight)
            {
                if (_yDirection == NumberingDirection.BottomToTop)
                {
                    // Слева направо, снизу вверх
                    return risers
                        .OrderBy(r => Math.Round(centers[r].X, 5))
                        .ThenBy(r => Math.Round(centers[r].Y, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
                else
                {
                    // Слева направо, сверху вниз
                    return risers
                        .OrderBy(r => Math.Round(centers[r].X, 5))
                        .ThenByDescending(r => Math.Round(centers[r].Y, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
            }
            else
            {
                if (_yDirection == NumberingDirection.BottomToTop)
                {
                    // Справа налево, снизу вверх
                    return risers
                        .OrderByDescending(r => Math.Round(centers[r].X, 5))
                        .ThenBy(r => Math.Round(centers[r].Y, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
                else
                {
                    // Справа налево, сверху вниз
                    return risers
                        .OrderByDescending(r => Math.Round(centers[r].X, 5))
                        .ThenByDescending(r => Math.Round(centers[r].Y, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
            }
        }
        else
        {
            // Сначала сортируем по Y, затем по X
            if (_yDirection == NumberingDirection.BottomToTop)
            {
                if (_xDirection == NumberingDirection.LeftToRight)
                {
                    // Снизу вверх, слева направо
                    return risers
                        .OrderBy(r => Math.Round(centers[r].Y, 5))
                        .ThenBy(r => Math.Round(centers[r].X, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
                else
                {
                    // Снизу вверх, справа налево
                    return risers
                        .OrderBy(r => Math.Round(centers[r].Y, 5))
                        .ThenByDescending(r => Math.Round(centers[r].X, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
            }
            else
            {
                if (_xDirection == NumberingDirection.LeftToRight)
                {
                    // Сверху вниз, слева направо
                    return risers
                        .OrderByDescending(r => Math.Round(centers[r].Y, 5))
                        .ThenBy(r => Math.Round(centers[r].X, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
                else
                {
                    // Сверху вниз, справа налево
                    return risers
                        .OrderByDescending(r => Math.Round(centers[r].Y, 5))
                        .ThenByDescending(r => Math.Round(centers[r].X, 5))
                        .ThenBy(r => centers[r].Z)
                        .ThenBy(r => riserIds[r])
                        .ToList();
                }
            }
        }
    }
}