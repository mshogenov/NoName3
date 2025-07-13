using Autodesk.Revit.DB.Plumbing;
using NoNameApi.Extensions;

namespace MakeBreak.Models;

public class Gap
{
    public FamilyInstance FamilyInstance { get; set; }
    public List<Element> ConnectedElements => GetConnectedElements();
    public ElementId Id { get; set; }
    public List<Connector> Connectors => [..GetConnectors()];

    public Gap(Reference reference, Document doc)
    {
        if (reference == null)
        {
            return;
        }
        Element element = doc.GetElement(reference);
        switch (element)
        {
            case FamilyInstance familyInstance:
                FamilyInstance = familyInstance;
                Id = familyInstance.Id;
                break;
            case DisplacementElement displacement:
                var pickPoint = reference.GlobalPoint;
                FamilyInstance = FindElementInDisplacement(displacement, pickPoint);
                Id = FamilyInstance?.Id;
                break;
            default: return;
        }
    }

    private FamilyInstance FindElementInDisplacement(DisplacementElement displacement, XYZ pickPoint)
    {
        if (displacement == null || pickPoint == null) return null;
        Document doc = displacement.Document;
        FamilyInstance familyInstance = null;
        var displacementElementIds = displacement.GetDisplacedElementIds();
        double toleranceInMm = 100.0;

        // Преобразование миллиметров в внутренние единицы Revit (футы)
        double tolerance = UnitUtils.ConvertToInternalUnits(toleranceInMm, UnitTypeId.Millimeters);

        foreach (ElementId displacedId in displacementElementIds)
        {
            Element element = doc.GetElement(displacedId);

            if (element is not FamilyInstance instance) continue;

            BoundingBoxXYZ bounding = instance.get_BoundingBox(doc.ActiveView);
            if (bounding == null) continue;

            // Расширяем BoundingBox на величину погрешности
            XYZ expandVector = new XYZ(tolerance, tolerance, tolerance);
            BoundingBoxXYZ expandedBounding = new BoundingBoxXYZ
            {
                Min = bounding.Min.Subtract(expandVector),
                Max = bounding.Max.Add(expandVector)
            };

            var contains = expandedBounding.Contains(pickPoint);
            if (!contains) continue;

            familyInstance = instance;
            break;
        }

        return familyInstance;
    }

    private Gap(FamilyInstance familyInstance)
    {
        if (familyInstance == null) return;
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
                if (!connector.IsConnected) continue;
                foreach (Connector connectedConnector in connector.AllRefs.Cast<Connector>())
                {
                    if (connectedConnector.Owner.Id != this.Id)
                    {
                        elements.Add(connectedConnector.Owner);
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

            if (breaksInPath.Count != 0)
            {
                breakLists.Add(breaksInPath);
            }
        }

        switch (breakLists.Count)
        {
            // Остальная логика выбора парного разрыва...
            case 0:
                return null;
            case 1: return new Gap(breakLists[0][0]);
            default:
            {
                var oddList = breakLists.FirstOrDefault(list => list.Count % 2 != 0);
                return oddList != null ? new Gap(oddList[0]) : null;
            }
        }
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

        var elementsConnected = element.GetConnectedMEPElements()
            .Where(connectedElement => !visitedElements.Contains(connectedElement.Id));
        foreach (var connectedElement in elementsConnected)
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