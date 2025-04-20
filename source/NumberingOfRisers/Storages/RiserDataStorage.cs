using Autodesk.Revit.DB.Plumbing;
using NumberingOfRisers.Models;
using NumberingOfRisers.Services;

namespace NumberingOfRisers.Storages;

public class RiserDataStorage
{
    private readonly NumberingOfRisersServices _numberingOfRisersServices = new();
    public List<Riser> Risers = [];

    public RiserDataStorage(Document doc)
    {
        LoadRisers(doc);
      
    }

    public void LoadRisers(Document doc)
    {
        List<Pipe> verticalPipes = _numberingOfRisersServices.GetVerticalPipes(doc).ToList();
        var verticalPipesAlongLocations = verticalPipes.GroupBy(p => p, new PipeIEqualityComparer()).ToList();
        Risers = verticalPipesAlongLocations
            .Select(verticalPipesAlongLocation => new Riser(verticalPipesAlongLocation))
            .Where(x => x.TotalLength > 2500).ToList().OrderBy(x => x.Number).ToList();
    }
}