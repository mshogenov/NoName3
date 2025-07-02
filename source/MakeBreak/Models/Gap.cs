using Autodesk.Revit.DB.Plumbing;
using NoNameApi.Extensions;

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

    private List<Element> GetConnectedElements()
    {
        var elements = new List<Element>();
        try
        {
            foreach (var connector in Connectors)
            {
                if (connector.IsConnected)
                {
                    foreach (Connector connectedConnector in connector.AllRefs.Cast<Connector>())
                    {
                        if (connectedConnector.Owner.Id != this.Id)
                        {
                            elements.Add(connectedConnector.Owner);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
           
        }
        return elements;
    }

    private IEnumerable<Connector> GetConnectors()
    {
        if (FamilyInstance?.MEPModel?.ConnectorManager?.Connectors == null)
            yield break;

        foreach (Connector connector in FamilyInstance.MEPModel.ConnectorManager.Connectors)
        {
            yield return connector;
        }
    }

    public Gap FindPairBreak(FamilySymbol familySymbol)
    {
        var breakLists = new List<List<FamilyInstance>>();
        var visitedElements = new HashSet<ElementId>(); // Для отслеживания просмотренных элементов

        foreach (var element in ConnectedElements)
        {
            var breaksInPath = new List<FamilyInstance>();
            visitedElements.Clear(); // Очищаем для каждого нового пути
            visitedElements.Add(Id); // Добавляем исходный разрыв
            FindBreaksInPath(element, familySymbol, breaksInPath, 0, visitedElements);

            if (breaksInPath.Any())
            {
                breakLists.Add(breaksInPath);
            }
        }

        // Остальная логика выбора парного разрыва...
        if (!breakLists.Any()) return null;

        if (breakLists.Count == 1 && breakLists[0].Count == 1)
            return new Gap(breakLists[0][0]);

        var oddList = breakLists.FirstOrDefault(list => list.Count % 2 != 0);
        return oddList != null ? new Gap(oddList[0]) : null;
    }

    private void FindBreaksInPath(Element element, FamilySymbol familySymbol,
        List<FamilyInstance> breaks, int depth, HashSet<ElementId> visitedElements)
    {
        if (element == null || !visitedElements.Add(element.Id)) return;

        if (depth > 2 && breaks.Count == 0) return;

        if (element is FamilyInstance familyInstance && familyInstance.Name == familySymbol.Name)
        {
            breaks.Add(familyInstance);
            depth = 0;
        }

        foreach (var connectedElement in element.GetConnectedMEPElements()
                     .Where(connectedElement => !visitedElements.Contains(connectedElement.Id)))
        {
            FindBreaksInPath(connectedElement, familySymbol, breaks, depth + 1, visitedElements);
        }
    }


    public Pipe FindGeneralPipe(Gap gap)
    {
        foreach (var connectedElement in ConnectedElements)
        {
            if (connectedElement is not Pipe pipe) continue;
            if (gap.ConnectedElements.FirstOrDefault(x => x.Id == pipe.Id) is Pipe generalPipe)
            {
                return generalPipe;
            }
        }

        return null;
    }
}