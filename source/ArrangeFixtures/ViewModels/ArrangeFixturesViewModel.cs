using ArrangeFixtures.Filters;
using ArrangeFixtures.Services;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External.Handlers;
using Nice3point.Revit.Toolkit.Options;
using NoNameApi.Extensions;
using NoNameApi.Utils;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ArrangeFixtures.ViewModels;

public sealed partial class ArrangeFixturesViewModel : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private ActionEventHandler _actionEventHandler = new();
    [ObservableProperty] private int _selectedPipesCount = 0;
    [ObservableProperty] private List<Pipe> _pipes = [];
    [ObservableProperty] private List<Element> _fixtures = [];
    [ObservableProperty] private Element _selectedFixture;
    private ArrangeFixturesServices _services = new ArrangeFixturesServices();


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

        // Создаем словарь для хранения одного экземпляра каждого типа семейства
        var uniqueFixtures = new Dictionary<string, Element>();

        // Добавляем неиспользуемые типы семейств
        foreach (var fixture in unusedFixtures)
        {
            string familyAndTypeName = $"{fixture.FamilyName}_{fixture.Name}";
            if (!uniqueFixtures.ContainsKey(familyAndTypeName))
            {
                uniqueFixtures.Add(familyAndTypeName, fixture);
            }
        }

        // Добавляем по одному экземпляру каждого используемого семейства
        foreach (var fixture in fixtures)
        {
            if (nestedFamilyIds.Contains(fixture.Id))
                continue;

            string familyAndTypeName = $"{fixture.Symbol.FamilyName}_{fixture.Symbol.Name}";
            if (!uniqueFixtures.ContainsKey(familyAndTypeName))
            {
                uniqueFixtures.Add(familyAndTypeName, fixture);
            }
        }

        // Преобразуем в список
        Fixtures = uniqueFixtures.Values.ToList();
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

    [RelayCommand]
    private void ArrangeFixtures()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                _services.ArrangeFixtures(Pipes, SelectedFixture);
            }
            catch (Exception e)
            {
                // ignored
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand]
    private void ClearSelection()
    {
        Pipes.Clear();
        SelectedPipesCount = 0;
    }
}