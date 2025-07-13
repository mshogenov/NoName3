using Autodesk.Revit.DB.Plumbing;

namespace MakeBreak.Models;

public sealed class PipeWrp
{
    public Pipe Pipe { get; }
  
    public Curve Curve => (Pipe.Location as LocationCurve)?.Curve;
    public ElementId Id { get; }
    public Element ReferenceLevel => Pipe.ReferenceLevel;

    public Connector[] AllConnectors =>
        Pipe?.ConnectorManager?.Connectors?.Cast<Connector>().ToArray() ?? [];


    public PipeWrp(Pipe pipe)
    {
        Pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        Id = pipe.Id;
    }

    public XYZ ProjectPointOntoCurve(XYZ point, DisplacementElement displacement = null)
    {
        IntersectionResult res = displacement != null
            ? Curve.Project(point - displacement.GetAbsoluteDisplacement())
            : Curve.Project(point);
        return res.XYZPoint;
    }

    public IReadOnlyList<Connector> GetOpenConnectors() =>
        AllConnectors.Where(c => !c.IsConnected).ToList();

    /// <summary>
    /// Получает центральную точку трубы
    /// </summary>
    public XYZ GetPipeCenter() => Curve.Evaluate(0.5, true);

    public double? GetDiameter()
    {
        Parameter pipeDiameterParam = Pipe.FindParameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
        if (pipeDiameterParam is not { HasValue: true }) return null;
        return pipeDiameterParam.AsDouble();
    }

    public XYZ GetDirection() 
    {
        Line line = Curve as Line;
        if (line != null)
        {
            return line.Direction;
        }

        // Если кривая не линия, вычисляем направление между начальной и конечной точками
        if (Curve != null)
        {
            XYZ startPoint = Curve.GetEndPoint(0);
            XYZ endPoint = Curve.GetEndPoint(1);
            return (endPoint - startPoint).Normalize();
        }

        return XYZ.BasisZ; // Возвращаем вертикальное направление как значение по умолчанию
    }
}