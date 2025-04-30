namespace CopyAnnotations.Services;

public class XYZEqualityComparer : IEqualityComparer<XYZ>
{
    private const double Tolerance = 0.001;

    public bool Equals(XYZ? x, XYZ? y)
    {
        if (x == null || y == null)
            return x == y;

        return Math.Abs(x.X - y.X) < Tolerance &&
               Math.Abs(x.Y - y.Y) < Tolerance &&
               Math.Abs(x.Z - y.Z) < Tolerance;
    }

    public int GetHashCode(XYZ obj)
    {
        if (obj == null)
            return 0;

        // Округляем до 3 знаков для хеширования
        return HashCode.Combine(
            Math.Round(obj.X, 3),
            Math.Round(obj.Y, 3),
            Math.Round(obj.Z, 3)
        );
    }
}