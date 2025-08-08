using System.Windows;
using UpdatingParameters.Services;
using UpdatingParameters.Storages.Settings;

namespace UpdatingParameters.ViewModels.Settings
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsDataStorage _settingsDataStorage;
        private bool _updaterIsChecked;

        public bool UpdaterIsChecked
        {
            get => _updaterIsChecked;
            set
            {
                _updaterIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.UpdaterIsChecked = value;
            }
        }

        private bool _pipesWithoutIsChecked;

        public bool PipesWithoutIsChecked
        {
            get => _pipesWithoutIsChecked;
            set
            {
                _pipesWithoutIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.PipesWithoutIsChecked = value;
            }
        }

        private bool _pipesOuterDiameterIsChecked;

        public bool PipesOuterDiameterIsChecked
        {
            get => _pipesOuterDiameterIsChecked;
            set
            {
                _pipesOuterDiameterIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.PipesOuterDiameterIsChecked = value;
            }
        }

        private bool _pipesInternalDiameterIsChecked;

        public bool PipesInternalDiameterIsChecked
        {
            get => _pipesInternalDiameterIsChecked;
            set
            {
                _pipesInternalDiameterIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.PipesInternalDiameterIsChecked = value;
            }
        }

        private bool _flexPipesWithoutIsChecked;

        public bool FlexPipesWithoutIsChecked
        {
            get => _flexPipesWithoutIsChecked;
            set
            {
                _flexPipesWithoutIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.FlexPipesWithoutIsChecked = value;
            }
        }

        private bool _flexPipesConnectionsIsChecked;

        public bool FlexPipesConnectionsIsChecked
        {
            get => _flexPipesConnectionsIsChecked;
            set
            {
                _flexPipesConnectionsIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.FlexPipesConnectionsIsChecked = value;
            }
        }

        private bool _flexPipesCorrugationsIsChecked;

        public bool FlexPipesCorrugationsIsChecked
        {
            get => _flexPipesCorrugationsIsChecked;
            set
            {
                _flexPipesCorrugationsIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.FlexPipesCorrugationsIsChecked = value;
            }
        }

        private bool _pipeInsulationTubesIsChecked;

        public bool PipeInsulationTubesIsChecked
        {
            get => _pipeInsulationTubesIsChecked;
            set
            {
                _pipeInsulationTubesIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.PipeInsulationTubesIsChecked = value;
            }
        }

        private bool _pipeInsulationCylindersIsChecked;

        public bool PipeInsulationCylindersIsChecked
        {
            get => _pipeInsulationCylindersIsChecked;
            set
            {
                _pipeInsulationCylindersIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.PipeInsulationCylindersIsChecked = value;
            }
        }

        private bool _pipeInsulationColouredTubesIsChecked;

        public bool PipeInsulationColouredTubesIsChecked
        {
            get => _pipeInsulationColouredTubesIsChecked;
            set
            {
                _pipeInsulationColouredTubesIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.PipeInsulationColouredTubesIsChecked = value;
            }
        }

        private bool _ductWithoutIsChecked;

        public bool DuctWithoutIsChecked
        {
            get => _ductWithoutIsChecked;
            set
            {
                if (value == _ductWithoutIsChecked) return;
                _ductWithoutIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctWithoutIsChecked = value;
            }
        }

        private bool _ductRoundIsChecked;

        public bool DuctRoundIsChecked
        {
            get => _ductRoundIsChecked;
            set
            {
                if (value == _ductRoundIsChecked) return;
                _ductRoundIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctRoundIsChecked = value;
            }
        }

        private bool _ductPlasticIsChecked;

        public bool DuctPlasticIsChecked
        {
            get => _ductPlasticIsChecked;
            set
            {
                if (value == _ductPlasticIsChecked) return;
                _ductPlasticIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctPlasticIsChecked = value;
            }
        }

        private bool _ductRectangularIsChecked;

        public bool DuctRectangularIsChecked
        {
            get => _ductRectangularIsChecked;
            set
            {
                if (value == _ductRectangularIsChecked) return;
                _ductRectangularIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctRectangularIsChecked = value;
            }
        }

        private bool _flexibleDuctsRoundIsChecked;

        public bool FlexibleDuctsRoundIsChecked
        {
            get => _flexibleDuctsRoundIsChecked;
            set
            {
                if (value == _flexibleDuctsRoundIsChecked) return;
                _flexibleDuctsRoundIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.FlexibleDuctsRoundIsChecked = value;
            }
        }

        private bool _ductInsulationFireproofingIsChecked;

        public bool DuctInsulationFireproofingIsChecked
        {
            get => _ductInsulationFireproofingIsChecked;
            set
            {
                if (value == _ductInsulationFireproofingIsChecked) return;
                _ductInsulationFireproofingIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctInsulationFireproofingIsChecked = value;
            }
        }

        private bool _ductInsulationThermalInsulationIsChecked;

        public bool DuctInsulationThermalInsulationIsChecked
        {
            get => _ductInsulationThermalInsulationIsChecked;
            set
            {
                if (value == _ductInsulationThermalInsulationIsChecked) return;
                _ductInsulationThermalInsulationIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctInsulationThermalIsChecked = value;
            }
        }

        private bool _ductConnectionPartsIsChecked;

        public bool DuctConnectionPartsIsChecked
        {
            get => _ductConnectionPartsIsChecked;
            set
            {
                if (value == _ductConnectionPartsIsChecked) return;
                _ductConnectionPartsIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.DuctConnectionPartsIsChecked = value;
            }
        }

        private bool _adskSystemNameIsChecked;

        public bool AdskSystemNameIsChecked
        {
            get => _adskSystemNameIsChecked;
            set
            {
                _adskSystemNameIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.AdskSystemNameIsChecked = value;
            }
        }

        private bool _adskSystemAbbreviationIsChecked;

        public bool AdskSystemAbbreviationIsChecked
        {
            get => _adskSystemAbbreviationIsChecked;
            set
            {
                _adskSystemAbbreviationIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.AdskSystemAbbreviationIsChecked = value;
            }
        }

        private bool _adskWallThicknessIsChecked;

        public bool AdskWallThicknessIsChecked
        {
            get => _adskWallThicknessIsChecked;
            set
            {
                _adskWallThicknessIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.AdskWallThicknessIsChecked = value;
            }
        }

        private bool _hermeticClassIsChecked;

        public bool HermeticClassIsChecked
        {
            get => _hermeticClassIsChecked;
            set
            {
                if (value == _hermeticClassIsChecked) return;
                _hermeticClassIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.HermeticСlassIsChecked = value;
            }
        }

        private bool _setMarginIsChecked;

        public bool SetMarginIsChecked
        {
            get => _setMarginIsChecked;
            set
            {
                if (value == _setMarginIsChecked) return;
                _setMarginIsChecked = value;
                OnPropertyChanged();
                _settingsDataStorage.SetMarginIsChecked = value;
            }
        }

        public SettingsViewModel(SettingsDataStorage settingsDataStorage)
        {
            _settingsDataStorage = settingsDataStorage;
            UpdaterIsChecked = settingsDataStorage.UpdaterIsChecked;
            PipesWithoutIsChecked = settingsDataStorage.PipesWithoutIsChecked;
            PipesOuterDiameterIsChecked = settingsDataStorage.PipesOuterDiameterIsChecked;
            PipesInternalDiameterIsChecked = settingsDataStorage.PipesInternalDiameterIsChecked;
            FlexPipesWithoutIsChecked = settingsDataStorage.FlexPipesWithoutIsChecked;
            FlexPipesConnectionsIsChecked = settingsDataStorage.FlexPipesConnectionsIsChecked;
            FlexPipesCorrugationsIsChecked = settingsDataStorage.FlexPipesCorrugationsIsChecked;
            PipeInsulationTubesIsChecked = settingsDataStorage.PipeInsulationTubesIsChecked;
            PipeInsulationCylindersIsChecked = settingsDataStorage.PipeInsulationCylindersIsChecked;
            AdskSystemNameIsChecked = settingsDataStorage.AdskSystemNameIsChecked;
            AdskSystemAbbreviationIsChecked = settingsDataStorage.AdskSystemAbbreviationIsChecked;
            AdskWallThicknessIsChecked = settingsDataStorage.AdskWallThicknessIsChecked;
            HermeticClassIsChecked = settingsDataStorage.HermeticСlassIsChecked;
            DuctConnectionPartsIsChecked = settingsDataStorage.DuctConnectionPartsIsChecked;
            DuctInsulationThermalInsulationIsChecked = settingsDataStorage.DuctInsulationThermalIsChecked;
            DuctInsulationFireproofingIsChecked = settingsDataStorage.DuctInsulationFireproofingIsChecked;
            FlexibleDuctsRoundIsChecked = settingsDataStorage.FlexibleDuctsRoundIsChecked;
            DuctRectangularIsChecked = settingsDataStorage.DuctRectangularIsChecked;
            DuctPlasticIsChecked = settingsDataStorage.DuctPlasticIsChecked;
            DuctRoundIsChecked = settingsDataStorage.DuctRoundIsChecked;
            DuctWithoutIsChecked = settingsDataStorage.DuctWithoutIsChecked;
            PipeInsulationColouredTubesIsChecked = settingsDataStorage.PipeInsulationColouredTubesIsChecked;
            SetMarginIsChecked = settingsDataStorage.SetMarginIsChecked;
            SettingsManager.OnSettingsChanged += Update;
        }

        private void Update()
        {
            UpdaterIsChecked = _settingsDataStorage.UpdaterIsChecked;
            PipesWithoutIsChecked = _settingsDataStorage.PipesWithoutIsChecked;
            PipesOuterDiameterIsChecked = _settingsDataStorage.PipesOuterDiameterIsChecked;
            PipesInternalDiameterIsChecked = _settingsDataStorage.PipesInternalDiameterIsChecked;
            FlexPipesWithoutIsChecked = _settingsDataStorage.FlexPipesWithoutIsChecked;
            FlexPipesConnectionsIsChecked = _settingsDataStorage.FlexPipesConnectionsIsChecked;
            FlexPipesCorrugationsIsChecked = _settingsDataStorage.FlexPipesCorrugationsIsChecked;
            PipeInsulationTubesIsChecked = _settingsDataStorage.PipeInsulationTubesIsChecked;
            PipeInsulationCylindersIsChecked = _settingsDataStorage.PipeInsulationCylindersIsChecked;
            AdskSystemNameIsChecked = _settingsDataStorage.AdskSystemNameIsChecked;
            AdskSystemAbbreviationIsChecked = _settingsDataStorage.AdskSystemAbbreviationIsChecked;
            AdskWallThicknessIsChecked = _settingsDataStorage.AdskWallThicknessIsChecked;
            HermeticClassIsChecked = _settingsDataStorage.HermeticСlassIsChecked;
            DuctConnectionPartsIsChecked = _settingsDataStorage.DuctConnectionPartsIsChecked;
            DuctInsulationThermalInsulationIsChecked = _settingsDataStorage.DuctInsulationThermalIsChecked;
            DuctInsulationFireproofingIsChecked = _settingsDataStorage.DuctInsulationFireproofingIsChecked;
            FlexibleDuctsRoundIsChecked = _settingsDataStorage.FlexibleDuctsRoundIsChecked;
            DuctRectangularIsChecked = _settingsDataStorage.DuctRectangularIsChecked;
            DuctPlasticIsChecked = _settingsDataStorage.DuctPlasticIsChecked;
            DuctRoundIsChecked = _settingsDataStorage.DuctRoundIsChecked;
            DuctWithoutIsChecked = _settingsDataStorage.DuctWithoutIsChecked;
            PipeInsulationColouredTubesIsChecked = _settingsDataStorage.PipeInsulationColouredTubesIsChecked;
            SetMarginIsChecked = _settingsDataStorage.SetMarginIsChecked;
        }

        [RelayCommand]
        private void SaveSettings()
        {
            _settingsDataStorage.Save();
            MessageBox.Show("Настройки сохранены");
        }

        [RelayCommand]
        private void CloseWindow(object obj)
        {
            if (obj is Window window)
            {
                window.Close();
            }
        }

        [RelayCommand]
        private void ResetSettings()
        {
            SettingsManager.ResetSettings();
            MessageBox.Show("Настройки сброшены");
        }

        [RelayCommand]
        private void HighlightAll()
        {
            PipesWithoutIsChecked = true;
            PipesOuterDiameterIsChecked = true;
            PipesInternalDiameterIsChecked = true;
            FlexPipesWithoutIsChecked = true;
            FlexPipesConnectionsIsChecked = true;
            FlexPipesCorrugationsIsChecked = true;
            PipeInsulationTubesIsChecked = true;
            PipeInsulationCylindersIsChecked = true;
            AdskSystemNameIsChecked = true;
            AdskSystemAbbreviationIsChecked = true;
            HermeticClassIsChecked = true;
            AdskWallThicknessIsChecked = true;
            DuctConnectionPartsIsChecked = true;
            DuctInsulationThermalInsulationIsChecked = true;
            DuctInsulationFireproofingIsChecked = true;
            FlexibleDuctsRoundIsChecked = true;
            DuctRectangularIsChecked = true;
            DuctPlasticIsChecked = true;
            DuctRoundIsChecked = true;
            DuctWithoutIsChecked = true;
            PipeInsulationColouredTubesIsChecked = true;
            SetMarginIsChecked = true;
        }

        [RelayCommand]
        private void Deselect()
        {
            PipesWithoutIsChecked = false;
            PipesOuterDiameterIsChecked = false;
            PipesInternalDiameterIsChecked = false;
            FlexPipesWithoutIsChecked = false;
            FlexPipesConnectionsIsChecked = false;
            FlexPipesCorrugationsIsChecked = false;
            PipeInsulationTubesIsChecked = false;
            PipeInsulationCylindersIsChecked = false;
            AdskSystemNameIsChecked = false;
            AdskSystemAbbreviationIsChecked = false;
            HermeticClassIsChecked = false;
            AdskWallThicknessIsChecked = false;
            DuctConnectionPartsIsChecked = false;
            DuctInsulationThermalInsulationIsChecked = false;
            DuctInsulationFireproofingIsChecked = false;
            FlexibleDuctsRoundIsChecked = false;
            DuctRectangularIsChecked = false;
            DuctPlasticIsChecked = false;
            DuctRoundIsChecked = false;
            DuctWithoutIsChecked = false;
            PipeInsulationColouredTubesIsChecked = false;
            SetMarginIsChecked = false;
        }
    }
}