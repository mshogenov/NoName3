using CopyElementsByLevel.Models;
using CopyElementsByLevel.Services;
using CopyElementsByLevel.UserConfig;
using NoNameAPI;
using NoNameAPI.Services;
using System.Windows.Input;

namespace CopyElementsByLevel.DataContexts
{
    public partial class CopyToLevelsContext : ObservableObject
    {
        private readonly CopyMepService _copyMepService;
        private readonly string _titleDoc;
        private readonly UserSettingsService _userSettingsService;
        private readonly CopyToLevelsUserConfig _userConfig;
        private List<LevelWr> NotFilteredLevelItems { get; }
        private string _serchValue;
        private List<LevelWr> _filteredLevelItems = new List<LevelWr>();
        private int _countSelectedLevelItems;
        private bool HaveSelectedLevels => NotFilteredLevelItems.Any(l => l.IsSelected);
        public string SerchValue
        {
            get => _serchValue;
            set
            {
                if (value == _serchValue)
                    return;
                _serchValue = value;
                ExecuteFilter();
                OnPropertyChanged(nameof(SerchValue));
            }
        }
        public CopyToLevelsContext(CopyMepService copyMepService)
        {
            _copyMepService = copyMepService;
            _titleDoc = Context.UiApplication.ActiveUIDocument.Document.Title;
            _userSettingsService = new UserSettingsService(new NoNameConnector().Name);
            _userConfig = _userSettingsService.Get<CopyToLevelsUserConfig>();
            NotFilteredLevelItems = copyMepService.LevelWrs;
            ExecuteFilter();
            FillSelectedLevelsFromUserConfig();
            UpdateCountSelectedLevelItems();
        }
        public List<LevelWr> FilteredLevelItems
        {
            get => _filteredLevelItems;
            private set
            {
                if (object.Equals(value, _filteredLevelItems))
                    return;
                _filteredLevelItems = value;
                OnPropertyChanged(nameof(FilteredLevelItems));
            }
        }
        private void ExecuteFilter()
        {
            if (string.IsNullOrWhiteSpace(_serchValue))
            {
                List<LevelWr> filteredLevelItems = NotFilteredLevelItems;
                List<LevelWr> levelWrList = new List<LevelWr>(filteredLevelItems.Count);
                levelWrList.AddRange(filteredLevelItems);
                FilteredLevelItems = levelWrList;
            }
            else if (_serchValue == "*")
            {
                List<LevelWr> levelWrList = new List<LevelWr>();
                levelWrList.AddRange(NotFilteredLevelItems.Where(l => l.IsSelected));
                FilteredLevelItems = levelWrList;
            }
            else
            {
                List<LevelWr> levelWrList = new List<LevelWr>();
                levelWrList.AddRange(NotFilteredLevelItems.Where(p => p.Name.ToLower().Contains(_serchValue.ToLower())));
                FilteredLevelItems = levelWrList;
            }
        }
        private void FillSelectedLevelsFromUserConfig()
        {
            List<string> values;
            if (!_userConfig.TryGetValue(_titleDoc, out values))
                return;
            foreach (string str in values)
            {
                string l = str;
                LevelWr levelWr = NotFilteredLevelItems.FirstOrDefault(n => n.Title == l);
                if (levelWr != null)
                    levelWr.IsSelected = true;
            }
        }
        public int CountSelectedLevelItems
        {
            get => _countSelectedLevelItems;
            private set
            {
                if (value == _countSelectedLevelItems)
                    return;
                _countSelectedLevelItems = value;
                OnPropertyChanged(nameof(CountSelectedLevelItems));
            }
        }
        public ICommand UnChekedLevelsCommand => new RelayCommand(() => SafeExecute.Execute(() =>
        {
            FilteredLevelItems.Where(l => l.IsSelected).ToList().ForEach(l => l.IsSelected = false);
            UpdateCountSelectedLevelItems();
        }));
        [RelayCommand] private void UpdateCountSelectedLevelItems() => CountSelectedLevelItems = NotFilteredLevelItems.Count(l => l.IsSelected);
        public ICommand CopyElementsToLevelCommand => new RelayCommand(() => _copyMepService.CopyMepElementsToLevels(NotFilteredLevelItems.Where(l => l.IsSelected)));
    }
}
