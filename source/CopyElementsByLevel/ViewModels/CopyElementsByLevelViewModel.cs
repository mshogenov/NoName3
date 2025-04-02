using CopyElementsByLevel.Models;

namespace CopyElementsByLevel.ViewModels
{
    public sealed partial class CopyElementsByLevelViewModel : ObservableObject
    {
        [ObservableProperty] private List<LevelWr> _levelModels;
        private List<ElemWr> elementModels = new List<ElemWr>();
        public CopyElementsByLevelViewModel()
        {
            _levelModels = new List<LevelWr>();
            var levelsDoc = new FilteredElementCollector(Context.ActiveDocument).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().Cast<Level>().OrderBy(x => x.Elevation);

        }

    }
}