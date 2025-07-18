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
    [ObservableProperty] private bool _systemAbbreviationIsChecked = true;
    [ObservableProperty] private FamilySymbol _systemAbbreviationMarkSelected;
    [ObservableProperty] private bool _pipeInsulationIsChecked;
    [ObservableProperty] private FamilySymbol _pipeInsulationMarkSelected;
    private readonly Document _doc = Context.ActiveDocument;
    private readonly PlacementOfStampsServices _placementOfStampsServices = new();
    private List<PipeWrp> _pipes = [];
    private List<TagWrp> _existingTags;

    public PlacementOfStampsViewModel()
    {
        var activeView = _doc.ActiveView;
        var collectorPipes = new FilteredElementCollector(_doc, activeView.Id)
            .OfClass(typeof(Pipe))
            .WhereElementIsNotElementType()
            .Cast<Pipe>()
            .ToList();

        var displacementElements = new FilteredElementCollector(_doc, activeView.Id)
            .OfClass(typeof(DisplacementElement))
            .WhereElementIsNotElementType()
            .Cast<DisplacementElement>();
        var processedPipeIds = new HashSet<ElementId>();
        foreach (var displacement in displacementElements)
        {
            var displacementPipes = displacement.GetDisplacedElementIds()
                .Select(x => _doc.GetElement(x))
                .Where(x => x is Pipe)
                .Cast<Pipe>().ToList();

            foreach (var pipe in displacementPipes)
            {
                if (!processedPipeIds.Add(pipe.Id)) continue; // Add возвращает true, если элемент был добавлен
                _pipes.Add(new PipeWrp(pipe)
                {
                    IsDisplaced = true,
                    DisplacedPoint = displacement.GetAbsoluteDisplacement()
                });
            }
        }

        // Добавляем оставшиеся трубы
        foreach (var pipe in collectorPipes)
        {
            if (processedPipeIds.Add(pipe.Id))
            {
                _pipes.Add(new PipeWrp(pipe));
            }
        }

        _pipeMarks =
        [
            ..new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_PipeTags)
                .WhereElementIsElementType()
                .ToElements()
        ];


        _existingTags = _placementOfStampsServices.GetExistingAnnotations(_doc, activeView)
            .Cast<IndependentTag>()
            .Select(existingAnnotation => new TagWrp(existingAnnotation))
            .ToList();
    }

    // Вспомогательный метод для получения координат трубы
    private XYZ GetPipeLocation(Pipe pipe)
    {
        if (pipe.Location is LocationCurve locationCurve)
        {
            // Возвращаем центральную точку кривой
            return locationCurve.Curve.Evaluate(0.5, true);
        }
        else if (pipe.Location is LocationPoint locationPoint)
        {
            return locationPoint.Point;
        }

        // Если не удалось получить координаты, возвращаем начало координат
        return XYZ.Zero;
    }

    [RelayCommand]
    private void PlacementMarks()
    {
        // var pipesOuterDiameters = elements.Where(p =>
        //     p.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString() == "Днар х Стенка");
        using TransactionGroup transactionGroup = new(_doc, "Расстановка марок");
        transactionGroup.Start();
        try
        {
            // if (PipesOuterDiametersIsChecked)
            // {
            //     _placementOfStampsServices.PlacementMarksPipesOuterDiameters(_doc, pipeMdls, activeView,PipesOuterDiameterMarkSelected);
            // }

            if (SystemAbbreviationIsChecked)
            {
                Transaction transaction = new Transaction(_doc, "Расставить сокращения");
                transaction.Start();
                _placementOfStampsServices.PlacementMarksSystemAbbreviation(_pipes, _existingTags,
                    SystemAbbreviationMarkSelected);
                transaction.Commit();
            }

            // if (PipeInsulationIsChecked)
            // {
            //     _placementOfStampsServices.PlacementMarksPipeInsulation(_doc, pipeMdls, activeView,
            //         PipeInsulationMarkSelected);
            // }

            transactionGroup.Commit();
          
        }
        catch (Exception e)
        {
            transactionGroup.RollBack();
            TaskDialog.Show("Ошибка", e.Message);
        }
    }
}