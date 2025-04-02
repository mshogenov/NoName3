using NumberingOfRisers.Models;
using NumberingOfRisers.Services;

namespace NumberingOfRisers.Storages
{
    public class SettingsDataStorage
    {
        public bool ManualFillingIsChecked { get; set; }
        public bool AutomaticFillingIsChecked { get; set; }
        public double MinimumPipeLengthRiserPipe { get; set; }
        public int MinimumNumberPipesRiserPipe { get; set; }
        private readonly JsonDataLoader _dataLoader;
        public SettingsDataStorage()
        {
            _dataLoader = new JsonDataLoader("SettingsDataStorage");
            Load();
        }

        public void Save()
        {
            var dto = new SettingsDTO
            {
                ManualFillingIsChecked = ManualFillingIsChecked,
                MinimumNumberPipesRiserPipe = MinimumNumberPipesRiserPipe,
                MinimumPipeLengthRiserPipe = MinimumPipeLengthRiserPipe,
                AutomaticFillingIsChecked = AutomaticFillingIsChecked,
            };
            _dataLoader.SaveData(dto);
        }

        public void InitializeDefault()
        {
            ManualFillingIsChecked = true;
            AutomaticFillingIsChecked = false;
            MinimumPipeLengthRiserPipe = 1200;
            MinimumNumberPipesRiserPipe = 3;
        }

        public void Load()
        {
            var loaded = _dataLoader.LoadData<SettingsDTO>();
            if (loaded == null)
            {
                InitializeDefault();
            }
            else
            {
                ManualFillingIsChecked = loaded.ManualFillingIsChecked;
                MinimumNumberPipesRiserPipe = loaded.MinimumNumberPipesRiserPipe;
                MinimumPipeLengthRiserPipe = loaded.MinimumPipeLengthRiserPipe;
                AutomaticFillingIsChecked = loaded.AutomaticFillingIsChecked;
            }
        }

    }
}
