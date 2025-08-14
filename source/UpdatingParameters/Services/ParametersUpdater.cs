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
        private PipeInsulationColouredTubesDataStorage _pipeInsulationColoredTubesDataStorage;
        private DataStorageFactory _storageFactory;
        private SetMarginDataStorage _setMarginDataStorage;
        private const string SetParamSystemAbbreviation = "ADSK_Система_Сокращение";
        private const string GetParamSystemAbbreviation = "Сокращение для системы";
        private const string SetParamSystemName = "ADSK_Система_Имя";
        private const string GetParamSystemName = "Имя системы";

        public ParametersUpdater()
        {
            InitializeStorages();
            SettingsDataStorage.OnSettingsDataChanged += SettingsDataStorageOnSettingsDataChanged;
            DataStorageFormulas.OnDataStorageFormulasChanged += DataStorageFormulas_OnDataStorageFormulasChanged;
            ParametersDataStorage.OnParametersDataStorageChanged += ParametersDataStorageOnParametersDataStorageChanged;
            SetMarginDataStorage.OnSetMarginDataStorageChanged += SetMarginDataStorageChanged;
        }

        private void InitializeStorages()
        {
            _storageFactory = new DataStorageFactory();
            _storageFactory.InitializeAllStorages();
            _pipesWithoutDataStorage = _storageFactory.GetStorage<PipesWithoutDataStorage>();
            _pipesOuterDiameterDataStorage = _storageFactory.GetStorage<PipesOuterDiameterDataStorage>();
            _pipesInternalDiameterDataStorage =
                _storageFactory.GetStorage<PipesInternalDiameterDataStorage>();
            _flexPipeWithoutDataStorage = _storageFactory.GetStorage<FlexPipeWithoutDataStorage>();
            _flexPipesCorrugationsDataStorage =
                _storageFactory.GetStorage<FlexPipesCorrugationsDataStorage>();
            _flexPipesConnectionsDataStorage =
                _storageFactory.GetStorage<FlexPipesConnectionsDataStorage>();
            _pipeInsulationCylindersDataStorage =
                _storageFactory.GetStorage<PipeInsulationCylindersDataStorage>();
            _pipeInsulationTubesDataStorage = _storageFactory.GetStorage<PipeInsulationTubesDataStorage>();
            _pipeInsulationColoredTubesDataStorage =
                _storageFactory.GetStorage<PipeInsulationColouredTubesDataStorage>();
            _ductInsulationFireproofingDataStorage =
                _storageFactory.GetStorage<DuctInsulationFireproofingDataStorage>();
            _ductInsulationThermalDataStorage =
                _storageFactory.GetStorage<DuctInsulationThermalDataStorage>();
            _ductConnectionPartsDataStorage = _storageFactory.GetStorage<DuctConnectionPartsDataStorage>();
            _ductPlasticDataStorage = _storageFactory.GetStorage<DuctPlasticDataStorage>();
            _ductRectangularDataStorage = _storageFactory.GetStorage<DuctRectangularDataStorage>();
            _ductRoundDataStorage = _storageFactory.GetStorage<DuctRoundDataStorage>();
            _ductWithoutDataStorage = _storageFactory.GetStorage<DuctWithoutDataStorage>();
            _flexibleDuctsRoundDataStorage = _storageFactory.GetStorage<FlexibleDuctsRoundDataStorage>();
            _settingsDataStorage = _storageFactory.GetStorage<SettingsDataStorage>();
            _ductParametersDataStorage = _storageFactory.GetStorage<DuctParametersDataStorage>();
            _parametersDataStorage = _storageFactory.GetStorage<ParametersDataStorage>();
            _setMarginDataStorage = _storageFactory.GetStorage<SetMarginDataStorage>();
        }

        private void ParametersDataStorageOnParametersDataStorageChanged(object sender, EventArgs e)
        {
            _storageFactory.UpdateStorage<ParametersDataStorage>();
        }

        private void SetMarginDataStorageChanged(object sender, EventArgs e)
        {
            _storageFactory.UpdateStorage<SetMarginDataStorage>();
            _setMarginDataStorage.UpdateData();
        }

        private void DataStorageFormulas_OnDataStorageFormulasChanged(object sender, EventArgs e)
        {
            switch (sender)
            {
                case PipesWithoutDataStorage:
                    _storageFactory.UpdateStorage<PipesWithoutDataStorage>();
                    break;
                case PipesInternalDiameterDataStorage:

                    _storageFactory.UpdateStorage<PipesInternalDiameterDataStorage>();
                    break;
                case PipesOuterDiameterDataStorage:

                    _storageFactory.UpdateStorage<PipesOuterDiameterDataStorage>();
                    break;
                case FlexPipeWithoutDataStorage:

                    _storageFactory.UpdateStorage<FlexPipeWithoutDataStorage>();
                    break;
                case FlexPipesConnectionsDataStorage:

                    _storageFactory.UpdateStorage<FlexPipesConnectionsDataStorage>();
                    break;
                case FlexPipesCorrugationsDataStorage:

                    _storageFactory.UpdateStorage<FlexPipesCorrugationsDataStorage>();
                    break;
                case PipeInsulationCylindersDataStorage:

                    _storageFactory.UpdateStorage<PipeInsulationCylindersDataStorage>();
                    break;
                case PipeInsulationTubesDataStorage:

                    _storageFactory.UpdateStorage<PipeInsulationTubesDataStorage>();
                    break;
                case DuctInsulationFireproofingDataStorage:
                    _storageFactory.UpdateStorage<DuctInsulationFireproofingDataStorage>();
                    break;
                case DuctInsulationThermalDataStorage:
                    _storageFactory.UpdateStorage<DuctInsulationThermalDataStorage>();
                    break;
                case DuctConnectionPartsDataStorage:
                    _storageFactory.UpdateStorage<DuctConnectionPartsDataStorage>();
                    break;
                case DuctPlasticDataStorage:
                    _storageFactory.UpdateStorage<DuctPlasticDataStorage>();
                    break;
                case DuctRectangularDataStorage:
                    _storageFactory.UpdateStorage<DuctRectangularDataStorage>();
                    break;
                case DuctRoundDataStorage:
                    _storageFactory.UpdateStorage<DuctRoundDataStorage>();
                    break;
                case DuctWithoutDataStorage:
                    _storageFactory.UpdateStorage<DuctWithoutDataStorage>();
                    break;
                case FlexibleDuctsRoundDataStorage:
                    _storageFactory.UpdateStorage<FlexibleDuctsRoundDataStorage>();
                    break;
                case DuctParametersDataStorage:
                    _storageFactory.UpdateStorage<DuctParametersDataStorage>();
                    break;
            }
        }

        private void SettingsDataStorageOnSettingsDataChanged()
        {
            _storageFactory.UpdateStorage<SettingsDataStorage>();
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


                    if (_settingsDataStorage.SetMarginIsChecked)
                    {
                        if (SetMarginDataStorage.MarginUpdateCallCount == 2)
                        {
                            _setMarginDataStorage.UpdateData();
                        }

                        foreach (var marginCategory in _setMarginDataStorage.MarginCategories)
                        {
                            if (!marginCategory.IsChecked) continue;
                            if (element.Category.Id != marginCategory.Category.Id) continue;

                            // Получаем параметры у конкретного элемента
                            var fromParam = element.FindParameter(marginCategory.FromParameterName);
                            var fromValue = fromParam.AsDouble();
                            double newValue = (fromValue / 100) * marginCategory.Margin + fromValue;
                            if (marginCategory.IsCopyInParameter)
                            {
                                var inParam = element.FindParameter(marginCategory.InParameterName);
                                if (inParam == null ||
                                    fromParam.StorageType != StorageType.Double ||
                                    inParam.IsReadOnly) continue;
                                inParam.Set(newValue);
                            }
                            else
                            {
                                if (fromParam.StorageType != StorageType.Double) continue;
                                fromParam.Set(newValue);
                            }
                        }
                        SetMarginDataStorage.MarginUpdateCallCount++;
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