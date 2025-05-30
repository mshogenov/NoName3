using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace SystemModelingCommands.Models;

public class ConnectorWrapper
{
    public Connector Connector { get; set; }
    public Element Owner => Connector.Owner;
    public Element ConnectedElement => GetConnectedElement();

    public Transform CoordinateSystem => Connector.CoordinateSystem;
    public XYZ Origin => Connector.Origin;
    public bool IsConnected => Connector.IsConnected;
    public Connector ConnectedConnector => GetConnectedConnector();
    public int Id { get; set; }
    public Domain Domain { get; set; }
    public double Radius => Connector.Radius;
    public ConnectorProfileType Shape { get; set; }
    public double Width => Connector.Width;
    public double Height => Connector.Height;

    public ConnectorWrapper(Connector connector)
    {
        Connector = connector;
        Id = connector.Id;
        Domain = connector.Domain;
        Shape = connector.Shape;
        
    }


    private Connector GetConnectedConnector()
    {
        if (!IsConnected)
            return null;

        return Connector.AllRefs
            .Cast<Connector>().Where(x =>
                x.Owner is not PipingSystem && x.Owner is not DuctInsulation && x.Owner is not PipeInsulation)
            .FirstOrDefault(IsValidConnectedConnector);
    }

    private bool IsValidConnectedConnector(Connector other)
    {
        if (other == null)
            return false;

        // Проверяем, что коннекторы действительно соединены
        const double tolerance = 0.001; // допустимая погрешность в футах
        bool sameLocation = Origin.DistanceTo(other.Origin) < tolerance;

        // Проверяем, что это не тот же самый коннектор
        bool differentConnectors = other.Owner.Id != Owner.Id;

        // Проверяем домен (например, что это физическое соединение)
        bool validDomain = other.Domain == Domain.DomainPiping ||
                           other.Domain == Domain.DomainHvac;

        return sameLocation && differentConnectors && validDomain;
    }

    private bool IsPhysicalConnector(Connector connector)
    {
        if (connector == null)
            return false;

        try
        {
            return connector.ConnectorType == ConnectorType.Physical;
        }
        catch
        {
            return false;
        }
    }

    private Element GetConnectedElement()
    {
        return IsConnected ? Connector.AllRefs.Cast<Connector>().FirstOrDefault()?.Owner : null;
    }
}