using Autodesk.Revit.DB.Plumbing;

namespace MakeBreak.Models;

public sealed class PipeWrapper
{
    private Pipe Pipe { get; }
    public bool IsDisplacement { get; set; }
    private Curve Curve => (Pipe.Location as LocationCurve)?.Curve;
    public ElementId Id { get; }

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
}