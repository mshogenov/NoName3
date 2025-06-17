namespace MepElementsCopy.Models;

public class ConnectorSplitModel
{
    public ElementId IdMepCurve { get; }

    public XYZ Direction => Connector.CoordinateSystem.BasisZ;

    private XYZ Origin => Connector.Origin;

    public Connector Connector { get; }

    public ElementId OwnerId { get; }

    public bool AreOpposite(Connector connector) =>
        Math.Abs(Direction.DotProduct(connector.CoordinateSystem.BasisZ) + 1.0) < 0.001;

    public double GetDistance(ConnectorSplitModel connector) => Origin.DistanceTo(connector.Origin);

    public ConnectorSplitModel(Connector connector, ElementId idMepCurve)
    {
        Connector = connector;
        IdMepCurve = idMepCurve;
        OwnerId = connector.Owner.Id;
    }
}