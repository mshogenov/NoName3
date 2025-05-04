using System.CodeDom.Compiler;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using PlacementOfStamps.Models;
using PlacementOfStamps.Services;

namespace PlacementOfStamps.ViewModels;

public sealed partial class PlacementOfStampsViewModel : ObservableObject
{
    [ObservableProperty] private bool _pipesOuterDiametersIsChecked;
    [ObservableProperty] private FamilySymbol _pipesOuterDiameterMarkSelected;
    [ObservableProperty] private HashSet<Element> _pipeMarks;
    [ObservableProperty] private bool _systemAbbreviationIsChecked=true;
    [ObservableProperty] private FamilySymbol _systemAbbreviationMarkSelected;
    [ObservableProperty] private bool _pipeInsulationIsChecked;
    [ObservableProperty] private FamilySymbol _pipeInsulationMarkSelected;
    private readonly Document _doc;
    private PlacementOfStampsServices _placementOfStampsServices;

    public PlacementOfStampsViewModel()
    {
        _doc = Context.ActiveDocument;

        _pipeMarks =
        [
            ..new FilteredElementCollector(_doc).OfCategory(BuiltInCategory.OST_PipeTags)
                .WhereElementIsElementType().ToElements()
        ];
    }

    [RelayCommand]
    private void PlacementMarks()
    {
        var activeView = _doc.ActiveView;
        _placementOfStampsServices = new PlacementOfStampsServices();
       
        var collectorPipes = new FilteredElementCollector(_doc, activeView.Id)
            .OfClass(typeof(Pipe)).WhereElementIsNotElementType().Where(p =>
                p.FindParameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble().ToMillimeters() > 500).ToList();
        FilteredElementCollector collectorDisplacement =
            new FilteredElementCollector(_doc, activeView.Id).OfClass(typeof(DisplacementElement))
                .WhereElementIsNotElementType();
        List<Element> displacedElements = [];
        List<PipeMdl> pipeMdls = [];
        displacedElements.AddRange(collectorDisplacement
            .Where(element => element.Category.BuiltInCategory == BuiltInCategory.OST_DisplacementElements));

        if (displacedElements.Count != 0)
        {
            foreach (var el in displacedElements)
            {
                DisplacementElement displacementElement = el as DisplacementElement;
                XYZ pointDisplaced = displacementElement?.GetRelativeDisplacement();
                var displacedElementIds = displacementElement?.GetDisplacedElementIds();
                if (displacedElementIds == null) continue;
                foreach (var elementId in displacedElementIds)
                {
                    var displacedElementFamily = _doc.GetElement(elementId);
                    if (displacedElementFamily is not Pipe pipeDisplaced) continue;
                    foreach (var pipe in collectorPipes.Where(p => pipeDisplaced.Id == p.Id).ToList())
                    {
                        collectorPipes.Remove(pipe);
                        pipeMdls.Add(new PipeMdl(pipeDisplaced)
                        {
                            IsDisplaced = true,
                            PointDisplaced = pointDisplaced,
                        });
                    }
                }
            }
        }

        pipeMdls.AddRange(collectorPipes.Select(pipe => new PipeMdl(pipe)));

        // var pipesOuterDiameters = elements.Where(p =>
        //     p.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString() == "Днар х Стенка");
        using Transaction tr = new(_doc, "Расстановка марок");
        tr.Start();
        try
        {
            // if (PipesOuterDiametersIsChecked)
            // {
            //     _placementOfStampsServices.PlacementMarksPipesOuterDiameters(_doc, pipeMdls, activeView,PipesOuterDiameterMarkSelected);
            // }

            if (SystemAbbreviationIsChecked)
            {
                _placementOfStampsServices.PlacementMarksSystemAbbreviation(_doc, pipeMdls, activeView,
                    SystemAbbreviationMarkSelected);
            }

            // if (PipeInsulationIsChecked)
            // {
            //     _placementOfStampsServices.PlacementMarksPipeInsulation(_doc, pipeMdls, activeView,
            //         PipeInsulationMarkSelected);
            // }

            tr.Commit();
        }
        catch (Exception e)
        {
            tr.RollBack();
            TaskDialog.Show("Ошибка", e.Message);
        }
    }
}