using ArrangeFixtures.Filters;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.Options;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ArrangeFixtures.ViewModels;

public sealed partial class ArrangeFixturesViewModel : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    [ObservableProperty] private int _selectedPipesCount = 0;
    [ObservableProperty] private List<Pipe> _pipes = [];
    [ObservableProperty] private List<FamilyInstance> _fixtures;

    public ArrangeFixturesViewModel()
    {
        _fixtures = new FilteredElementCollector(_doc).OfCategory(BuiltInCategory.OST_CableTrayFitting)
            .WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
    }

    [RelayCommand]
    private void SelectedPipe()
    {
        try
        {
            var r = _uidoc.Selection.PickObjects(ObjectType.Element, new MEPCurveSelectionFilter());

            foreach (var reference in r)
            {
                Pipes.Add(_doc.GetElement(reference) as Pipe);
            }

            SelectedPipesCount = Pipes.Count;
        }
        catch (OperationCanceledException ex)
        {
        }
        catch
        {
        }
    }
}