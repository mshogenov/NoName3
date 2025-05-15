using Autodesk.Revit.DB.Plumbing;

namespace MakeBreak.Models;

public sealed class PipeWrapper
{
    public Pipe Pipe { get; }
    public bool IsDisplacement { get; set; }
    public Curve Curve => (Pipe.Location as LocationCurve)?.Curve;
    public ElementId Id { get; }
    public Element ReferenceLevel => Pipe.ReferenceLevel;

    private IEnumerable<Connector> AllConnectors =>
        Pipe?.ConnectorManager?.Connectors?.Cast<Connector>() ?? [];


    public PipeWrapper(Pipe pipe)
    {
        Pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        Id = pipe.Id;
    }

    public XYZ ProjectPointOntoCurve(XYZ point, DisplacementElement displacement = null)
    {
        IntersectionResult res = displacement != null
            ? Curve.Project(point - displacement.GetRelativeDisplacement())
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

    public XYZ GetDirection() => (Curve as Line)?.Direction;
}