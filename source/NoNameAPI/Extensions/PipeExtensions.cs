using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace NoNameApi.Extensions;

public static class PipeExtensions
{
    /// <summary>
    /// Находит центральную точку трубы
    /// </summary>
    /// <param name="pipe">Элемент трубы</param>
    /// <returns>Координаты центральной точки трубы</returns>
    public static XYZ GetPipeCenter(this Pipe pipe)
    {
        var curve = (pipe.Location as LocationCurve)?.Curve;
        return curve?.Evaluate(0.5, true);
    }

    /// <summary>
    /// Определяет направление трубы в виде вектора
    /// </summary>
    /// <param name="pipe">Элемент трубы</param>
    /// <returns>Вектор направления трубы</returns>
    public static XYZ GetPipeDirection(this Pipe pipe)
    {
        var curve = (pipe.Location as LocationCurve)?.Curve;
        if (curve == null) return null;
        XYZ startPoint = curve.GetEndPoint(0);
        XYZ endPoint = curve.GetEndPoint(1);
        return (endPoint - startPoint).Normalize();
    }
}