namespace MepElementsCopy.Models;

public class ConnectorSplitModel
{
    public ElementId IdMepCurve { get; }

    public XYZ Direction { get; }

    public XYZ Origin { get; }

    public Connector Connector { get; }

    public ElementId OwnerId { get; }

    public bool AreOpposite(Connector connector) => Math.Abs(this.Direction.DotProduct(connector.CoordinateSystem.BasisZ) + 1.0) < 0.001;

    public double GetDistance(ConnectorSplitModel connector) => this.Origin.DistanceTo(connector.Origin);

    public ConnectorSplitModel(Connector connector, ElementId idMepCurve)
    {
       Connector = connector;
       IdMepCurve = idMepCurve;
       OwnerId = connector.Owner.Id;
       Direction = connector.CoordinateSystem.BasisZ;
       Origin = connector.Origin;
    }
}