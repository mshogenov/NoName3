using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Settings
{
    public class SettingsDataStorage : IDataStorage
    {
        public static event Action OnSettingsDataChanged;

        public bool UpdaterIsChecked { get; set; }

        public bool PipesWithoutIsChecked { get; set; }
        public bool PipesOuterDiameterIsChecked { get; set; }
        public bool PipesInternalDiameterIsChecked { get; set; }
        public bool FlexPipesWithoutIsChecked { get; set; }
        public bool FlexPipesConnectionsIsChecked { get; set; }
        public bool FlexPipesCorrugationsIsChecked { get; set; }
        public bool PipeInsulationTubesIsChecked { get; set; }
        public bool PipeInsulationCylindersIsChecked { get; set; }
        public bool AdskSystemNameIsChecked { get; set; }
        public bool AdskSystemAbbreviationIsChecked { get; set; }
        public bool AdskWallThicknessIsChecked { get; set; }
        public bool PipeInsulationColouredTubesIsChecked { get; set; }
        public bool DuctWithoutIsChecked { get; set; }
        public bool DuctRoundIsChecked { get; set; }
        public bool DuctPlasticIsChecked { get; set; }
        public bool DuctRectangularIsChecked { get; set; }
        public bool FlexibleDuctsRoundIsChecked { get; set; }
        public bool DuctInsulationFireproofingIsChecked { get; set; }
        public bool DuctInsulationThermalIsChecked { get; set; }
        public bool DuctConnectionPartsIsChecked { get; set; }
        public bool HermeticСlassIsChecked { get; set; }
        public bool SetMarginIsChecked { get; set; }

        private readonly IDataLoader _dataLoader;
        public SettingsDataStorage(IDataLoader dataLoader)
        {
            _dataLoader = dataLoader;
            LoadData();
        }
        public void Save()
        {
            var dto = new SettingsDto
            {
                UpdaterIsChecked = UpdaterIsChecked,
                PipesWithoutIsChecked = PipesWithoutIsChecked,
                PipesOuterDiameterIsChecked = PipesOuterDiameterIsChecked,
                PipesInternalDiameterIsChecked = PipesInternalDiameterIsChecked,
                FlexPipesWithoutIsChecked = FlexPipesWithoutIsChecked,
                FlexPipesConnectionsIsChecked = FlexPipesConnectionsIsChecked,
                FlexPipesCorrugationsIsChecked = FlexPipesCorrugationsIsChecked,
                PipeInsulationTubesIsChecked = PipeInsulationTubesIsChecked,
                PipeInsulationCylindersIsChecked = PipeInsulationCylindersIsChecked,
                AdskSystemNameIsChecked = AdskSystemNameIsChecked,
                AdskSystemAbbreviationIsChecked = AdskSystemAbbreviationIsChecked,
                AdskWallThicknessIsChecked=AdskWallThicknessIsChecked,
                PipeInsulationColouredTubesIsChecked=PipeInsulationColouredTubesIsChecked,
                DuctWithoutIsChecked=DuctWithoutIsChecked,
                DuctRoundIsChecked=DuctRoundIsChecked,
                DuctPlasticIsChecked=DuctPlasticIsChecked,
                DuctRectangularIsChecked=DuctRectangularIsChecked,
                FlexibleDuctsRoundIsChecked=FlexibleDuctsRoundIsChecked,
                DuctInsulationFireproofingIsChecked=DuctInsulationFireproofingIsChecked,
                DuctInsulationThermalInsulationIsChecked=DuctInsulationThermalIsChecked,
                DuctConnectionPartsIsChecked=DuctConnectionPartsIsChecked,
                HermeticClassIsChecked=HermeticСlassIsChecked,
                SetMarginIsChecked=SetMarginIsChecked,
            };
            _dataLoader.SaveData(dto);
            OnSettingsDataChanged?.Invoke();
        }

        public void InitializeDefault()
        {
            UpdaterIsChecked = false;
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
            AdskWallThicknessIsChecked = false;
            PipeInsulationColouredTubesIsChecked = false;
            DuctWithoutIsChecked=false;
            DuctRoundIsChecked=false;
            DuctPlasticIsChecked=false;
            DuctRectangularIsChecked=false;
            FlexibleDuctsRoundIsChecked=false;
            DuctInsulationFireproofingIsChecked=false;
            DuctInsulationThermalIsChecked=false;
            DuctConnectionPartsIsChecked=false;
            HermeticСlassIsChecked=false;
            SetMarginIsChecked=false;
            Save();
        }

        public void UpdateData()
        {
           LoadData();
        }

        private void LoadData()
        {
            var loaded = _dataLoader.LoadData<SettingsDto>();
            if (loaded == null)
            {
                InitializeDefault();
            }
            else
            {
                UpdaterIsChecked = loaded.UpdaterIsChecked;
                PipesWithoutIsChecked = loaded.PipesWithoutIsChecked;
                PipesOuterDiameterIsChecked = loaded.PipesOuterDiameterIsChecked;
                PipesInternalDiameterIsChecked = loaded.PipesInternalDiameterIsChecked;
                FlexPipesWithoutIsChecked = loaded.FlexPipesWithoutIsChecked;
                FlexPipesConnectionsIsChecked = loaded.FlexPipesConnectionsIsChecked;
                FlexPipesCorrugationsIsChecked = loaded.FlexPipesCorrugationsIsChecked;
                PipeInsulationTubesIsChecked = loaded.PipeInsulationTubesIsChecked;
                PipeInsulationCylindersIsChecked = loaded.PipeInsulationCylindersIsChecked;
                AdskSystemNameIsChecked = loaded.AdskSystemNameIsChecked;
                AdskSystemAbbreviationIsChecked = loaded.AdskSystemAbbreviationIsChecked;
                AdskWallThicknessIsChecked=loaded.AdskWallThicknessIsChecked;
                PipeInsulationColouredTubesIsChecked = loaded.PipeInsulationColouredTubesIsChecked;
                DuctWithoutIsChecked=loaded.DuctWithoutIsChecked;
                DuctRoundIsChecked=loaded.DuctRoundIsChecked;
                DuctPlasticIsChecked=loaded.DuctPlasticIsChecked;
                DuctRectangularIsChecked=loaded.DuctRectangularIsChecked;
                FlexibleDuctsRoundIsChecked=loaded.FlexibleDuctsRoundIsChecked;
                DuctInsulationFireproofingIsChecked=loaded.DuctInsulationFireproofingIsChecked;
                DuctInsulationThermalIsChecked=loaded.DuctInsulationThermalInsulationIsChecked;
                DuctConnectionPartsIsChecked=loaded.DuctConnectionPartsIsChecked;
                HermeticСlassIsChecked = loaded.HermeticClassIsChecked;
                SetMarginIsChecked = loaded.SetMarginIsChecked;
            }
        }


    }
}
