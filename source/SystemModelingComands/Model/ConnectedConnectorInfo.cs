namespace SystemModelingCommands.Model;

public class ConnectedConnectorInfo
{
    public Connector ConnectedConnector { get; set; }
    public Element ConnectedElement { get; set; }

    public ConnectedConnectorInfo(Connector connectedConnector, Element connectedElement)
    {
        ConnectedConnector = connectedConnector;
        ConnectedElement = connectedElement;
    }
}