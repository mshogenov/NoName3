using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace NoNameApi.Extensions;

public static class PipeExtensions
{
    /// <summary>
    /// Получает центральную точку трубы
    /// </summary>
    /// <param name="pipe"></param>
    public static XYZ GetPipeCenter(this Pipe pipe)
    {
        if (pipe.Location is not LocationCurve locationCurve)
            return (pipe.Location as LocationPoint)?.Point ?? XYZ.Zero;
        Curve curve = locationCurve.Curve;
        XYZ startPoint = curve.GetEndPoint(0);
        XYZ endPoint = curve.GetEndPoint(1);
        // Вычисляем центральную точку
        return (startPoint + endPoint) * 0.5;
    }
}