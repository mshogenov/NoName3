using CopyByLevel.Models;
using CopyByLevel.Services;
using System.Windows.Input;

namespace CopyByLevel.ViewModels
{

    public sealed partial class CopyByLevelViewModel : ObservableObject
    {
        private readonly CopyMepService _copyMepService;
      [ObservableProperty]  private List<LevelWr> _filteredLevelItems = new List<LevelWr>();
       [ObservableProperty] private string _serchValue;
        private List<LevelWr> NotFilteredLevelItems { get; }

        [ObservableProperty] private int _countSelectedLevelItems;
        private readonly string _titleDoc;

        public CopyByLevelViewModel(CopyMepService copyMepService)
        {
           _copyMepService = copyMepService;
           _titleDoc = Context.UiApplication.ActiveUIDocument.Document.Title;


          NotFilteredLevelItems = copyMepService.LevelWrs;
            ExecuteFilter();

            //UpdateCountSelectedLevelItems();
        }

        [RelayCommand]
        public void CopyElementsToLevel()
        {
            _copyMepService.CopyMepElementsToLevels(NotFilteredLevelItems.Where(l => l.IsSelected));
        }
        //public ICommand CopyElementsToLevelCommand => new RelayCommand((Action)(() => SafeExecute.Execute((Action)(() => this._copyMepService.CopyMepElementsToLevels(this.NotFilteredLevelItems.Where(l => l.IsSelected))))), (Func<object, bool>)(_ => this.HaveSelectedLevels));

        //public ICommand SelectLevelItemCommand => new RelayCommand((Action)(() => SafeExecute.Execute(new Action(this.UpdateCountSelectedLevelItems))));

        //public ICommand OnWindowClosed => (ICommand)new RelayCommand((Action)(() => SafeExecute.Execute((Action)(() =>
        //{
        //    this._userConfig.AddItem(this._titleDoc, (IEnumerable<string>)this.NotFilteredLevelItems.Where<LevelWr>((Func<LevelWr, bool>)(l => l.IsSelected)).Select<LevelWr, string>((Func<LevelWr, string>)(l => l.Title)).ToList<string>());
        //    this._userSettingsService.Set((object)this._userConfig);
        //}))));

        //public ICommand ShowChekedLevelsCommand => new RelayCommand((Action)(() => SafeExecute.Execute((Action)(() => this.SerchValue = this._serchValue == "*" ? string.Empty : "*"))));

        //public ICommand UnChekedLevelsCommand => new RelayCommand((Action)(() => SafeExecute.Execute((Action)(() =>
        //{
        //    this.FilteredLevelItems.Where<LevelWr>(l => l.IsSelected).ToList<LevelWr>().ForEach(l => l.IsSelected = false);
        //    this.UpdateCountSelectedLevelItems();
        //}))));



        private bool HaveSelectedLevels => this.NotFilteredLevelItems.Any<LevelWr>(l => l.IsSelected);



        private void ExecuteFilter()
        {
            if (string.IsNullOrWhiteSpace(_serchValue))
            {
                List<LevelWr> filteredLevelItems = this.NotFilteredLevelItems;
                List<LevelWr> levelWrList = new List<LevelWr>(filteredLevelItems.Count);
                levelWrList.AddRange(filteredLevelItems);
                this.FilteredLevelItems = levelWrList;
            }
            else if (_serchValue == "*")
            {
                List<LevelWr> levelWrList = new List<LevelWr>();
                levelWrList.AddRange(this.NotFilteredLevelItems.Where<LevelWr>(l => l.IsSelected));
                this.FilteredLevelItems = levelWrList;
            }
            else
            {
                List<LevelWr> levelWrList = new List<LevelWr>();
                levelWrList.AddRange(this.NotFilteredLevelItems.Where<LevelWr>(p => p.Name.ToLower().Contains(this._serchValue.ToLower())));
                this.FilteredLevelItems = levelWrList;
            }
        }

        private void UpdateCountSelectedLevelItems() => CountSelectedLevelItems = NotFilteredLevelItems.Count(l => l.IsSelected);
    }
}