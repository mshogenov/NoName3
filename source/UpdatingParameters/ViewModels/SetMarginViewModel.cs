using System.Collections.ObjectModel;
using UpdatingParameters.Models;
using UpdatingParameters.Views;

namespace UpdatingParameters.ViewModels;

public partial class SetMarginViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<MarginCategory> _marginCategories = [];
    [RelayCommand]
    private void AddCategory()
    {
       
        AddCategoryWindow addCategoryWindow = new AddCategoryWindow();
        AddCategoryVM addCategoryVM = new AddCategoryVM();
        addCategoryWindow.DataContext = addCategoryVM;
        // Показываем диалог и проверяем результат
        if (addCategoryWindow.ShowDialog() == true && addCategoryVM.IsConfirmed)
        {
            MarginCategories.Add(addCategoryVM.Result);
        }
    }
}