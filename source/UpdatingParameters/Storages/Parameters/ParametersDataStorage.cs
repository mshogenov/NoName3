using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Parameters
{
    public class ParametersDataStorage : DataStorageBase
    {
        public static event EventHandler OnParametersDataStorageChanged;
        private readonly IDataLoader _dataLoader;
        public bool SystemAbbreviationIsChecked { get; set; }
        public bool SystemNameIsChecked { get; set; }
        public bool WallThicknessIsChecked { get; set; }
       public bool HermeticClassIsChecked { get; set; }

        public ParametersDataStorage(IDataLoader dataLoader)
        {
            _dataLoader = dataLoader;
            LoadData();
        }
        public override void InitializeDefault()
        {
            SystemAbbreviationIsChecked = true;
            SystemNameIsChecked = true;
            WallThicknessIsChecked = false;
            HermeticClassIsChecked = false;
            Save();
        }
        public sealed override void LoadData()
        {
            var loaded = _dataLoader.LoadData<ParametersDto>();
            if (loaded != null)
            {
                SystemAbbreviationIsChecked = loaded.SystemAbbreviationIsChecked;
                SystemNameIsChecked = loaded.SystemNameIsChecked;
                WallThicknessIsChecked = loaded.WallThicknessIsChecked;
                HermeticClassIsChecked = loaded.HermeticClassIsChecked;
            }
            else
            {
                InitializeDefault();
            }
        }
        public override void Save()
        {
            var dto = new ParametersDto
            {
                SystemAbbreviationIsChecked = SystemAbbreviationIsChecked,
                SystemNameIsChecked= SystemNameIsChecked,
                WallThicknessIsChecked= WallThicknessIsChecked,
                HermeticClassIsChecked = HermeticClassIsChecked
                
            };
            _dataLoader.SaveData(dto);
            OnParametersDataStorageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
