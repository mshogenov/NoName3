namespace SystemModelingCommands.Model;

public class ConnectorConnection
{
    public Connector Connector { get; set; }
    public Element Element { get; set; }
    public List<ConnectorConnection> ConnectedConnectors { get; set; } = [];

    public ConnectorConnection(Connector connector)
    {
        Connector = connector;
        Element = connector.Owner;
    }
}