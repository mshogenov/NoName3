using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using UpdatingParameters.Services;
using UpdatingParameters.Storages.Pipes;

namespace UpdatingParameters.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private List<PipeType> _pipesOuterDiameterTypes=[];
    [ObservableProperty] private List<PipeType> _pipesWithoutTypes=[];
    [ObservableProperty] private List<PipeType> _pipesInternalDiameterTypes=[];
    [ObservableProperty] private List<FlexPipeType> _flexPipesWithoutTypes=[];
    [ObservableProperty] private List<FlexPipeType> _flexPipesConnectionsTypes=[];
    [ObservableProperty] private List<FlexPipeType> _flexiblePipesCorrugationsTypes=[];
    [ObservableProperty] private List<PipeInsulationType> _pipeInsulationTubesTypes=[];
    [ObservableProperty] private List<PipeInsulationType> _pipeInsulationCylindersTypes=[];
    [ObservableProperty] private List<PipeInsulationType> _pipeInsulationColouredTubesTypes=[];
    [ObservableProperty] private List<DuctType> _ductWithoutTypes=[];
    [ObservableProperty] private List<DuctType> _ductRoundTypes=[];
    [ObservableProperty] private List<DuctType> _ductPlasticTypes=[];
    [ObservableProperty] private List<DuctType> _ductRectangularTypes=[];
    [ObservableProperty] private List<FlexDuctType> _flexibleDuctsRoundTypes=[];
    [ObservableProperty] private List<DuctInsulationType> _ductInsulationFireproofingTypes=[];
    [ObservableProperty] private List<DuctInsulationType> _ductInsulationThermalInsulationTypes=[];
    public MainViewModel()
    {
        var pipesOuterDiameterDataStorage = DataStorageFactory.Instance.GetStorage<PipesOuterDiameterDataStorage>();
        
    }

   
}