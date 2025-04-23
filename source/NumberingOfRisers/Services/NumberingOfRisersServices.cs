using Autodesk.Revit.DB.Plumbing;
using NumberingOfRisers.Models;

namespace NumberingOfRisers.Services;

public class NumberingOfRisersServices
{
    public IEnumerable<Pipe> GetVerticalPipes(Document doc)
    {
        var pipeCollector = new FilteredElementCollector(doc)
            .OfClass(typeof(Pipe))
            .WhereElementIsNotElementType()
            .Cast<Pipe>();
        List<Pipe> verticalPipes = new List<Pipe>();

        // Выявляем вертикальные трубы (стояки)
        foreach (Pipe pipe in pipeCollector)
        {
            // Проверка на вертикальность (может быть разной логики)
            if (pipe.Location is not LocationCurve location) continue;
            Curve curve = location.Curve;
            XYZ direction = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

            // Если труба вертикальная (направление по Z)
            if (Math.Abs(direction.Z) > 0.9)
            {
                verticalPipes.Add(pipe);
            }
        }

        return verticalPipes;
    }

    public bool IsIdenticalId(IList<long> idValues1, IList<long> idValues2, double minMatchPercentage = 50.0)
    {
        if (idValues1 == null || idValues2 == null)

            return false;
        // Подсчитываем количество совпадающих ElementIds
        int matchCount = idValues1.Count(elementId =>
            idValues2.Any(otherElementId =>
                otherElementId == elementId));
        // Находим меньшее из количеств ElementIds двух стояков
        int minElementCount = Math.Min(idValues1.Count, idValues2.Count);
        // Вычисляем процент совпадений относительно меньшего количества ElementIds
        double matchPercentage = (matchCount * 100.0) / minElementCount;

        return matchPercentage >= minMatchPercentage;
    }
    
    /// Проверка, находится ли труба рядом с указанной точкой в плоскости XY
    public bool IsNearbyInXY(Pipe pipe, XYZ referencePoint, double tolerance)
    {
        XYZ pipeLocation = GetPipeLocationXY(pipe);
        // Вычисляем расстояние только по X и Y (игнорируем Z)
        double distance = Math.Sqrt(
            Math.Pow(pipeLocation.X - referencePoint.X, 2) +
            Math.Pow(pipeLocation.Y - referencePoint.Y, 2));

        return distance <= tolerance;
    }
    /// Получение XY-координат трубы (игнорируя Z)
    public XYZ GetPipeLocationXY(Pipe pipe)
    {
        if (pipe.Location is not LocationCurve locationCurve) return XYZ.Zero;
        // Берем среднюю точку трубы
        Curve curve = locationCurve.Curve;
        XYZ midPoint = curve.Evaluate(0.5, true);
        // Возвращаем только X и Y координаты, игнорируя Z
        return new XYZ(midPoint.X, midPoint.Y, 0);
    }
    
    /// Метод для проверки, находится ли точка близко к стояку
    public bool IsLocationCloseToRiser(XYZ location, Riser riser, double toleranceFeet)
    {
        if (!riser.Pipes.Any()) return false;

        // Проверяем все трубы стояка
        foreach (var pipe in riser.Pipes)
        {
            XYZ pipeLocation =GetPipeLocationXY(pipe);
            if (pipeLocation.DistanceTo(location) <= toleranceFeet)
            {
                return true;
            }
        }

        return false;
    }
    /// Проверяет не входят ли найденные трубы уже в существующие стояки
    public bool IsPipeAlreadyInExistingRiser(IEnumerable<Pipe> pipes, IEnumerable<RiserSystemType> riserSystemTypes)
    {
        foreach (var systemType in riserSystemTypes)
        {
            if (systemType.Risers.Any(r =>
                    pipes.Any(p => r.ElementIds.Contains(p.Id))))
            {
                return true;
            }
        }

        return false;
    }
    
    public Riser FindIdenticalExistingRiser(List<Pipe> pipes, List<Riser> risers)
    {
        foreach (Riser riser in risers)
        {
            if (IsIdenticalId(riser.ElementIds.Select(x=>x.Value).ToList(), pipes.Select(x => x.Id.Value).ToList()))
            {
                return riser;
            }
        }

        return null;
    }
    public void AddExistingRiserToSystem(Riser riser, List<RiserSystemType> riserSystemTypes)
    {
        string systemTypeName = riser.MepSystemType?.Name ?? "Без системы";
        RiserSystemType targetSystemType =
            riserSystemTypes.FirstOrDefault(st => st.MepSystemTypeName == systemTypeName);

        if (targetSystemType == null)
        {
            // Создаем новый тип системы, если такого еще нет
            targetSystemType = new RiserSystemType(new List<Riser> { riser });
            riserSystemTypes.Add(targetSystemType);
        }
        else if (!targetSystemType.Risers.Contains(riser))
        {
            targetSystemType.Risers.Add(riser);
        }
    }
    
    public void AddNewRiserToSystem(Riser newRiser,  List<RiserSystemType> riserSystemTypes, List<Riser> risers)
    {
        string systemTypeName = newRiser.MepSystemType?.Name ?? "Без системы";
        RiserSystemType targetSystemType =
            riserSystemTypes.FirstOrDefault(st => st.MepSystemTypeName == systemTypeName);

        if (targetSystemType == null)
        {
            // Создаем новый тип системы, если такого еще нет
            targetSystemType = new RiserSystemType(new List<Riser> { newRiser });
            riserSystemTypes.Add(targetSystemType);
        }
        else
        {
            targetSystemType.Risers.Add(newRiser);
        }

        // Добавляем новый стояк в хранилище данных
        risers.Add(newRiser);
    }
    
    /// Метод для нахождения ближайшего стояка к заданной точке
    public Riser FindNearestRiser(XYZ location, List<RiserSystemType> riserSystemTypes)
    {
        Riser nearest = null;
        double minDistance = double.MaxValue;

        foreach (var systemType in riserSystemTypes)
        {
            foreach (var riser in systemType.Risers)
            {
                // Используем первую трубу стояка для определения его расположения
                if (riser.Pipes.Any())
                {
                    XYZ riserLocation = GetPipeLocationXY(riser.Pipes.First());
                    double distance = riserLocation.DistanceTo(location);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = riser;
                    }
                }
            }
        }

        return nearest;
    }
}