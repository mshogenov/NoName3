using ArrangeFixtures.Filters;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.Options;
using NoNameApi.Utils;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ArrangeFixtures.ViewModels;

public sealed partial class ArrangeFixturesViewModel : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    [ObservableProperty] private int _selectedPipesCount = 0;
    [ObservableProperty] private List<Pipe> _pipes = [];
    [ObservableProperty] private List<Element> _fixtures = [];

    public ArrangeFixturesViewModel()
    {
        ISet<ElementId> unusedElements = _doc.GetUnusedElements(new HashSet<ElementId>()
        {
            new(BuiltInCategory.OST_CableTrayFitting)
        });

        // Получаем неиспользуемые семейства
        List<FamilySymbol> unusedFixtures = [];
        foreach (ElementId id in unusedElements)
        {
            if (_doc.GetElement(id) is FamilySymbol element)
                unusedFixtures.Add(element);
        }

        // Получаем используемые экземпляры
        var fixtures = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_CableTrayFitting)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Where(fi => !fi.Symbol.Family.IsInPlace)
            .Where(fi => fi.Host == null)
            .ToList();

        var nestedFamilies = Helpers.GetNestedFamilies(_doc, fixtures);
        var nestedFamilyIds = nestedFamilies.Select(e => e.Id).ToHashSet();
        // Создаем общий список, исключая вложенные семейства
        var allFixtures = new HashSet<Element>();

        // Добавляем неиспользуемые типы семейств
        allFixtures.UnionWith(unusedFixtures);
        foreach (var fixture in fixtures)
        {
            if (nestedFamilyIds.Contains(fixture.Id))
                continue;
            allFixtures.Add(fixture);
        }

        // Если нужен список вместо HashSet
        Fixtures = allFixtures.ToList();
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