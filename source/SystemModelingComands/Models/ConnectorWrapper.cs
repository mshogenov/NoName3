namespace SystemModelingCommands.Models;

public class ConnectorWrapper
{
    public Connector Connector { get; set; }
    public Element Owner => Connector.Owner;
    public Element ConnectedElement => GetConnectedElement();

    public Transform CoordinateSystem => Connector.CoordinateSystem;
    private List<ConnectorWrapper> _connectedConnectors = [];
    public XYZ Origin => Connector.Origin;
    public bool IsConnected => Connector.IsConnected;

    public ConnectorWrapper(Connector connector)
    {
        Connector = connector;
    }

    public Connector ConnectedConnector => GetConnectedConnector();

    private Connector GetConnectedConnector()
    {
        if (!IsConnected)
            return null;

        return Connector.AllRefs
            .Cast<Connector>()
            .FirstOrDefault(IsValidConnectedConnector);
    }

    private bool IsValidConnectedConnector(Connector other)
    {
        if (other == null)
            return false;
        if (!IsPhysicalConnector(other))
        {
            return false;
        }

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

    // Методы для работы со списком подключенных коннекторов
    public void AddConnectedConnector(ConnectorWrapper connector)
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));

        if (!_connectedConnectors.Contains(connector))
            _connectedConnectors.Add(connector);
    }

    public void RemoveConnectedConnector(ConnectorWrapper connector)
    {
        _connectedConnectors.Remove(connector);
    }

    public void ClearConnectedConnectors()
    {
        _connectedConnectors.Clear();
    }

    public bool HasConnectedConnectors()
    {
        return _connectedConnectors.Any();
    }

    // Если нужно добавить несколько коннекторов
    public void AddConnectedConnectors(IEnumerable<ConnectorWrapper> connectors)
    {
        if (connectors == null)
            throw new ArgumentNullException(nameof(connectors));

        foreach (var connector in connectors)
        {
            AddConnectedConnector(connector);
        }
    }
}