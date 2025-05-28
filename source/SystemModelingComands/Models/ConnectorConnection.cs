namespace SystemModelingCommands.Models;

public class ConnectorConnection
{
    public Connector TargetConnector { get; set; }
    public Element Element { get; set; }
    private readonly List<ConnectorConnection> _connectorConnections = [];
    public IReadOnlyList<ConnectorConnection> ConnectedConnectors => _connectorConnections;

    public ConnectorConnection(Connector targetConnector)
    {
        TargetConnector = targetConnector;
        Element = targetConnector.Owner;
    }

    public void AddConnectedConnector(Connector connector)
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));
        if (!_connectorConnections.Contains(new ConnectorConnection(connector)))
            _connectorConnections.Add(new ConnectorConnection(connector));
       
    }
}