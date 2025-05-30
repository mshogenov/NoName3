namespace SystemModelingCommands.Models;

public sealed class ElementWrapper
{
    public Element Element { get; set; }
    public ElementId Id { get; set; }

    public ConnectorManager ConnectorManager => GetConnectorManager(Element);
    public List<ConnectorWrapper> Connectors => GetConnectors();
    public List<Element> ConnectedElements => GetConnectedElements();

    public ElementWrapper(Element element)
    {
        if (element == null) return;
        Element = element;
        Id = element.Id;
    }

    private List<Element> GetConnectedElements()
    {
        List<Element> elements = [];
        foreach (var connector in Connectors)
        {
            if (connector.ConnectedElement != null)
            {
                elements.Add(connector.ConnectedElement);
            }
        }

        return elements;
    }

    private List<ConnectorWrapper> GetConnectors()
    {
        return ConnectorManager.Connectors
            .Cast<Connector>()
            .Select(x => new ConnectorWrapper(x))
            .ToList();
    }

    private static ConnectorManager GetConnectorManager(Element element) => element switch
    {
        MEPCurve mep => mep.ConnectorManager,
        FamilyInstance fi => fi.MEPModel?.ConnectorManager,
        _ => null
    };

    /// <summary>
    ///  Вспомогательный метод для поиска ближайшего свободного соединителя
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Connector FindClosestFreeConnector(XYZ point) =>
        Connectors
            .Where(c => !c.IsConnected)
            .OrderBy(c => c.Origin.DistanceTo(point))
            .FirstOrDefault()?.Connector;

    public MEPCurveType DeterminingTypeOfPipeByFitting()
    {
        if (Element is not FamilyInstance) return null;
        Document doc = Element.Document;
        Element connectedConnector = ConnectedElements.FirstOrDefault();
        return connectedConnector!=null ? doc.GetElement(connectedConnector.GetTypeId()) as MEPCurveType : null;
    }
}