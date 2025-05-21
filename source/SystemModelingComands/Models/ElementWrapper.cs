namespace SystemModelingCommands.Models;

public sealed class ElementWrapper
{
    public Element Element { get; set; }
    public ElementId Id { get; set; }
    public ConnectorManager ConnectorManager { get; }


    public ElementWrapper(Element element)
    {
        if (element == null) return;
        Element = element;
        Id = element.Id;
        ConnectorManager = GetConnectorManager(element);
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
        ConnectorManager?
            .Connectors
            .Cast<Connector>()
            .Where(c => !c.IsConnected)
            .OrderBy(c => c.Origin.DistanceTo(point))
            .FirstOrDefault();
}