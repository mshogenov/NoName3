using CommunityToolkit.Mvvm.Input;
using DeleteViewFilters.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DeleteViewFilters.ViewModels
{
    public sealed partial class DeleteViewFiltersViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<FilterDescriptor> _filters;
      [ObservableProperty]  private ObservableCollection<FilterDescriptor> _selectedFilters;
        public DeleteViewFiltersViewModel() 
        {
            Document doc = Context.Document;
            var filters = new FilteredElementCollector(doc)
                          .WherePasses(new ElementClassFilter(typeof(FilterElement))).Cast<FilterElement>().Select(filter => new FilterDescriptor(filter));
           
            // Инициализация коллекцийObservableCollection классами
            _filters = new ObservableCollection<FilterDescriptor>(filters);
            _selectedFilters = new ObservableCollection<FilterDescriptor>();
        }
        // Метод для удаления выбранных фильтров
        [RelayCommand]
        public void DeleteSelectedFilters(Window window)
        {
            Document doc = Context.Document;
            using (Transaction trans = new Transaction(doc, "Delete Selected Filters"))
            {
                trans.Start();

                foreach (var filter in SelectedFilters)
                {
                    
                    if (filter != null)
                    {
                        if (filter.IsCheked) 
                        {
                            Filters.Remove(filter);
                            doc.Delete((ICollection<ElementId>)filter);
                        }
                       
                    }
                }

                trans.Commit();
                window.Close();
            }

          
        }
    }

}