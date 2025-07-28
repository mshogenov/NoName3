namespace PlacementOfStamps.Services;

public class DirectionEqualityComparer : IEqualityComparer<XYZ>
{
    private const double Tolerance = 1e-6;

    public bool Equals(XYZ x, XYZ y)
    {
        if (x == null || y == null) return false;

        // Проверяем как прямое, так и обратное направление
        return AreEqual(x, y) || AreEqual(x, y.Negate());
    }

    private bool AreEqual(XYZ a, XYZ b)
    {
        return Math.Abs(a.X - b.X) < Tolerance &&
               Math.Abs(a.Y - b.Y) < Tolerance &&
               Math.Abs(a.Z - b.Z) < Tolerance;
    }

    public int GetHashCode(XYZ obj)
    {
        // Используем абсолютные значения для одинакового хеша
        var x = Math.Round(Math.Abs(obj.X), 6);
        var y = Math.Round(Math.Abs(obj.Y), 6);
        var z = Math.Round(Math.Abs(obj.Z), 6);
        return HashCode.Combine(x, y, z);
    }
}