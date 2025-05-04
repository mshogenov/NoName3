namespace CopyAnnotations.Services;

public class XYZEqualityComparer : IEqualityComparer<XYZ>
{
    private readonly double _tolerance;

    public XYZEqualityComparer(double tolerance = 1e-6)
    {
        _tolerance = tolerance;
    }

    public bool Equals(XYZ x, XYZ y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;

        return Math.Abs(x.X - y.X) < _tolerance &&
               Math.Abs(x.Y - y.Y) < _tolerance &&
               Math.Abs(x.Z - y.Z) < _tolerance;
    }

    public int GetHashCode(XYZ obj)
    {
        if (obj == null)
            return 0;

        // Упрощенная версия хеш-кода для XYZ
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Math.Round(obj.X / _tolerance).GetHashCode();
            hash = hash * 23 + Math.Round(obj.Y / _tolerance).GetHashCode();
            hash = hash * 23 + Math.Round(obj.Z / _tolerance).GetHashCode();
            return hash;
        }
    }
}