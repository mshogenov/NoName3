using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.DuctInsulation;
using UpdatingParameters.Storages.Ducts;
using UpdatingParameters.Storages.FlexPipes;
using UpdatingParameters.Storages.Parameters;
using UpdatingParameters.Storages.PipeInsulationMtl;
using UpdatingParameters.Storages.Pipes;
using UpdatingParameters.Storages.Settings;

namespace UpdatingParameters.Services
{
    public class ParametersUpdater : IUpdater
    {
        private PipesWithoutDataStorage _pipesWithoutDataStorage;
        private PipesInternalDiameterDataStorage _pipesInternalDiameterDataStorage;
        private PipesOuterDiameterDataStorage _pipesOuterDiameterDataStorage;
        private FlexPipeWithoutDataStorage _flexPipeWithoutDataStorage;
        private FlexPipesConnectionsDataStorage _flexPipesConnectionsDataStorage;
        private FlexPipesCorrugationsDataStorage _flexPipesCorrugationsDataStorage;
        private PipeInsulationCylindersDataStorage _pipeInsulationCylindersDataStorage;
        private PipeInsulationTubesDataStorage _pipeInsulationTubesDataStorage;
        private ParametersDataStorage _parametersDataStorage;
        private SettingsDataStorage _settingsDataStorage;
        private DuctInsulationFireproofingDataStorage _ductInsulationFireproofingDataStorage;
        private DuctInsulationThermalDataStorage _ductInsulationThermalDataStorage;
        private DuctConnectionPartsDataStorage _ductConnectionPartsDataStorage;
        private DuctPlasticDataStorage _ductPlasticDataStorage;
        private DuctRectangularDataStorage _ductRectangularDataStorage;
        private DuctRoundDataStorage _ductRoundDataStorage;
        private DuctWithoutDataStorage _ductWithoutDataStorage;
        private FlexibleDuctsRoundDataStorage _flexibleDuctsRoundDataStorage;
        private DuctParametersDataStorage _ductParametersDataStorage;
        private readonly PipeInsulationColouredTubesDataStorage _pipeInsulationColoredTubesDataStorage;
        private const string SetParamSystemAbbreviation = "ADSK_Система_Сокращение";
        private const string GetParamSystemAbbreviation = "Сокращение для системы";
        private const string SetParamSystemName = "ADSK_Система_Имя";
        private const string GetParamSystemName = "Имя системы";

        public ParametersUpdater()
        {
            DataStorageFactory.Instance.InitializeAllStorages();
            _pipesWithoutDataStorage = DataStorageFactory.Instance.GetStorage<PipesWithoutDataStorage>();
            _pipesOuterDiameterDataStorage = DataStorageFactory.Instance.GetStorage<PipesOuterDiameterDataStorage>();
            _pipesInternalDiameterDataStorage =
                DataStorageFactory.Instance.GetStorage<PipesInternalDiameterDataStorage>();
            _flexPipeWithoutDataStorage = DataStorageFactory.Instance.GetStorage<FlexPipeWithoutDataStorage>();
            _flexPipesCorrugationsDataStorage =
                DataStorageFactory.Instance.GetStorage<FlexPipesCorrugationsDataStorage>();
            _flexPipesConnectionsDataStorage =
                DataStorageFactory.Instance.GetStorage<FlexPipesConnectionsDataStorage>();
            _pipeInsulationCylindersDataStorage =
                DataStorageFactory.Instance.GetStorage<PipeInsulationCylindersDataStorage>();
            _pipeInsulationTubesDataStorage = DataStorageFactory.Instance.GetStorage<PipeInsulationTubesDataStorage>();
            _pipeInsulationColoredTubesDataStorage =
                DataStorageFactory.Instance.GetStorage<PipeInsulationColouredTubesDataStorage>();
            _ductInsulationFireproofingDataStorage =
                DataStorageFactory.Instance.GetStorage<DuctInsulationFireproofingDataStorage>();
            _ductInsulationThermalDataStorage =
                DataStorageFactory.Instance.GetStorage<DuctInsulationThermalDataStorage>();
            _ductConnectionPartsDataStorage = DataStorageFactory.Instance.GetStorage<DuctConnectionPartsDataStorage>();
            _ductPlasticDataStorage = DataStorageFactory.Instance.GetStorage<DuctPlasticDataStorage>();
            _ductRectangularDataStorage = DataStorageFactory.Instance.GetStorage<DuctRectangularDataStorage>();
            _ductRoundDataStorage = DataStorageFactory.Instance.GetStorage<DuctRoundDataStorage>();
            _ductWithoutDataStorage = DataStorageFactory.Instance.GetStorage<DuctWithoutDataStorage>();
            _flexibleDuctsRoundDataStorage = DataStorageFactory.Instance.GetStorage<FlexibleDuctsRoundDataStorage>();
            _settingsDataStorage = DataStorageFactory.Instance.GetStorage<SettingsDataStorage>();
            _ductParametersDataStorage = DataStorageFactory.Instance.GetStorage<DuctParametersDataStorage>();
_parametersDataStorage = DataStorageFactory.Instance.GetStorage<ParametersDataStorage>();
            SettingsDataStorage.OnSettingsDataChanged += SettingsDataStorageOnSettingsDataChanged;
            DataStorageFormulas.OnDataStorageFormulasChanged += DataStorageFormulas_OnDataStorageFormulasChanged;
            ParametersDataStorage.OnParametersDataStorageChanged += ParametersDataStorageOnParametersDataStorageChanged;
        }

        private void ParametersDataStorageOnParametersDataStorageChanged(object sender, EventArgs e)
        {
            DataStorageFactory.Instance.UpdateStorage<ParametersDataStorage>();
        }

        private void DataStorageFormulas_OnDataStorageFormulasChanged(object sender, EventArgs e)
        {
            switch (sender)
            {
                case PipesWithoutDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<PipesWithoutDataStorage>();
                    break;
                case PipesInternalDiameterDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<PipesInternalDiameterDataStorage>();
                    break;
                case PipesOuterDiameterDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<PipesOuterDiameterDataStorage>();
                    break;
                case FlexPipeWithoutDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<FlexPipeWithoutDataStorage>();
                    break;
                case FlexPipesConnectionsDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<FlexPipesConnectionsDataStorage>();
                    break;
                case FlexPipesCorrugationsDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<FlexPipesCorrugationsDataStorage>();
                    break;
                case PipeInsulationCylindersDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<PipeInsulationCylindersDataStorage>();
                    break;
                case PipeInsulationTubesDataStorage:

                    DataStorageFactory.Instance.UpdateStorage<PipeInsulationTubesDataStorage>();
                    break;
                case DuctInsulationFireproofingDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctInsulationFireproofingDataStorage>();
                    break;
                case DuctInsulationThermalDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctInsulationThermalDataStorage>();
                    break;
                case DuctConnectionPartsDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctConnectionPartsDataStorage>();
                    break;
                case DuctPlasticDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctPlasticDataStorage>();
                    break;
                case DuctRectangularDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctRectangularDataStorage>();
                    break;
                case DuctRoundDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctRoundDataStorage>();
                    break;
                case DuctWithoutDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctWithoutDataStorage>();
                    break;
                case FlexibleDuctsRoundDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<FlexibleDuctsRoundDataStorage>();
                    break;
                case DuctParametersDataStorage:
                    DataStorageFactory.Instance.UpdateStorage<DuctParametersDataStorage>();
                    break;
            }
        }

        private void SettingsDataStorageOnSettingsDataChanged()
        {
            DataStorageFactory.Instance.UpdateStorage<SettingsDataStorage>();
        }

        public void Execute(UpdaterData data)
        {
            try
            {
                if (!_settingsDataStorage.UpdaterIsChecked) return;
                var doc = data.GetDocument();
                var ids = data.GetAddedElementIds().ToList();
                ids.AddRange(data.GetModifiedElementIds());

                foreach (var id in ids)
                {
                    var element = doc.GetElement(id);


                    if (_parametersDataStorage.SystemAbbreviationIsChecked &&
                        _settingsDataStorage.AdskSystemAbbreviationIsChecked)
                    {
                        UpdaterParametersService.CopyParameter(Context.ActiveDocument, element,
                            GetParamSystemAbbreviation, SetParamSystemAbbreviation);
                    }

                    if (_parametersDataStorage.SystemNameIsChecked && _settingsDataStorage.AdskSystemNameIsChecked)
                    {
                        UpdaterParametersService.CopyParameter(Context.ActiveDocument, element, GetParamSystemName,
                            SetParamSystemName);
                    }

                    if (_parametersDataStorage.WallThicknessIsChecked &&
                        _settingsDataStorage.AdskWallThicknessIsChecked)
                    {
                        UpdaterParametersService.SetWallThickness(element,
                            _ductParametersDataStorage.DuctParameters);
                    }

                    if (_parametersDataStorage.HermeticClassIsChecked &&
                        _settingsDataStorage.HermeticСlassIsChecked)
                    {
                        UpdaterParametersService.SetHermeticСlass(element);
                    }

                    switch (element)
                    {
                        case Pipe:
                            switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                            {
                                case "Днар х Стенка":
                                    if (_settingsDataStorage.PipesOuterDiameterIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _pipesOuterDiameterDataStorage);
                                    }

                                    break;
                                case "Ду х Стенка":
                                    if (_settingsDataStorage.PipesInternalDiameterIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _pipesInternalDiameterDataStorage);
                                    }

                                    break;
                                default:
                                    if (_settingsDataStorage.PipesWithoutIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element, _pipesWithoutDataStorage);
                                    }

                                    break;
                            }

                            break;
                        case FlexPipe:
                            switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                            {
                                case "Подводки":
                                    if (_settingsDataStorage.FlexPipesConnectionsIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _flexPipesConnectionsDataStorage);
                                    }

                                    break;
                                case "Гофры":
                                    if (_settingsDataStorage.FlexPipesCorrugationsIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _flexPipesCorrugationsDataStorage);
                                    }

                                    break;
                                default:
                                    if (_settingsDataStorage.FlexPipesWithoutIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _flexPipeWithoutDataStorage);
                                    }

                                    break;
                            }

                            break;
                        case PipeInsulation:
                            switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                            {
                                case "Трубки":
                                    if (_settingsDataStorage.PipeInsulationTubesIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _pipeInsulationTubesDataStorage);
                                    }

                                    break;
                                case "Цилиндры":
                                    if (_settingsDataStorage.PipeInsulationCylindersIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _pipeInsulationCylindersDataStorage);
                                    }

                                    break;
                                case "Трубки цветные":
                                    if (_settingsDataStorage.PipeInsulationColouredTubesIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _pipeInsulationColoredTubesDataStorage);
                                    }

                                    break;
                                default:
                                    if (_settingsDataStorage.FlexPipesWithoutIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _flexPipeWithoutDataStorage);
                                    }

                                    break;
                            }

                            break;
                        case Duct:
                            switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                            {
                                case "Пластик":
                                    if (_settingsDataStorage.DuctPlasticIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _ductPlasticDataStorage);
                                    }

                                    break;

                                case "Прямоугольные":
                                    if (_settingsDataStorage.DuctRectangularIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _ductRectangularDataStorage);
                                    }

                                    break;
                                case "Круглые":
                                    if (_settingsDataStorage.DuctRoundIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element, _ductRoundDataStorage);
                                    }

                                    break;
                                default:
                                    if (_settingsDataStorage.DuctWithoutIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _ductWithoutDataStorage);
                                    }

                                    break;
                            }

                            break;
                        case DuctInsulation:
                            switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                            {
                                case "Огнезащита":
                                    if (_settingsDataStorage.DuctInsulationFireproofingIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _ductInsulationFireproofingDataStorage);
                                    }

                                    break;
                                case "Теплоизоляция":
                                    if (_settingsDataStorage.DuctInsulationThermalIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _ductInsulationThermalDataStorage);
                                    }

                                    break;
                            }

                            break;
                        case FlexDuct:
                            switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                            {
                                case "Круглые":
                                    if (_settingsDataStorage.FlexibleDuctsRoundIsChecked)
                                    {
                                        UpdaterParametersService.UpdateParameters(element,
                                            _flexibleDuctsRoundDataStorage);
                                    }

                                    break;
                            }

                            break;
                        case FamilyInstance:
                            if (element.Category.BuiltInCategory == BuiltInCategory.OST_DuctFitting)
                            {
                                if (_settingsDataStorage.DuctConnectionPartsIsChecked)
                                {
                                    UpdaterParametersService.UpdateParameters(element,
                                        _ductConnectionPartsDataStorage);
                                }
                            }

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", e.Message);
            }
        }

        public string GetAdditionalInformation()
        {
            return string.Empty;
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.MEPSystems;
        }

        public UpdaterId GetUpdaterId()
        {
            return new UpdaterId(Context.Application.ActiveAddInId, new Guid("1910d466-0d2d-459a-9703-fbf666cbc787"));
        }

        public string GetUpdaterName()
        {
            return "ParametersUpdater";
        }
    }
}