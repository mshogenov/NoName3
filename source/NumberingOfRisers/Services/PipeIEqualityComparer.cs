namespace NumberingOfRisers.Services;


public class PipeIEqualityComparer : IEqualityComparer<Element>
{
    // Константа для допустимого расстояния между трубами (в единицах модели)
    private const double Tolerance = 0.1; // Можно настроить под ваши требования

    public bool Equals(Element firstElement, Element secondElement)
    {
        if (firstElement == null || secondElement == null)
            return false;

        if (firstElement.Location is not LocationCurve firstElementLocation || secondElement.Location is not LocationCurve secondElementLocation)
            return false;

        var firstStartPoint = firstElementLocation.Curve.GetEndPoint(0);
        var secondStartPoint = secondElementLocation.Curve.GetEndPoint(0);

        // Проверяем, находятся ли точки достаточно близко друг к другу
        double deltaX = Math.Abs(firstStartPoint.X - secondStartPoint.X);
        double deltaY = Math.Abs(firstStartPoint.Y - secondStartPoint.Y);

        // Трубы считаются в одной группе, если обе координаты X и Y близки
        return deltaX <= Tolerance && deltaY <= Tolerance;
    }

    public int GetHashCode(Element obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (obj.Location is not LocationCurve locationCurve)
            throw new ArgumentException("Object does not have a valid location curve", nameof(obj));

        var startPoint = locationCurve.Curve.GetEndPoint(0);

        // Округляем координаты до ячеек размером с TOLERANCE
        // для обеспечения совместимости с Equals
        double cellX = Math.Floor(startPoint.X / Tolerance);
        double cellY = Math.Floor(startPoint.Y / Tolerance);

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + cellX.GetHashCode();
            hash = hash * 31 + cellY.GetHashCode();
            return hash;
        }
    }
}









