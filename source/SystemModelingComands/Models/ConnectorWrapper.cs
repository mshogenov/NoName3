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

    // Только чтение коллекции извне
    public Connector ConnectedConnector => GetConnectedConnector();

    private Connector GetConnectedConnector()
    {
        return Connector.AllRefs.Cast<Connector>().FirstOrDefault();
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