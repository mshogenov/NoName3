namespace MakeBreak.Models;

public class Gap
{
    public FamilyInstance FamilyInstance { get; set; }
    public List<Element> ConnectedElements => GetConnectedElements();

    public ElementId Id { get; set; }
    public List<Connector> Connectors => [..GetConnectors()];


    public Gap(FamilyInstance familyInstance)
    {
        if (familyInstance == null)
        {
            return;
        }

        FamilyInstance = familyInstance;
        Id = familyInstance.Id;
    }

    private List<Connector> GetConnectors()
    {
        return FamilyInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>().ToList();
    }

    private List<Element> GetConnectedElements()
    {
        List<Element> elements = [];
        foreach (var connector in Connectors)
        {
            if (!connector.IsConnected ) continue;
            elements.Add(connector.AllRefs.Cast<Connector>().First().Owner); 
        }

        return elements;
    }
}