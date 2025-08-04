using UpdatingParameters.Views;

namespace UpdatingParameters.ViewModels;

public partial class SetMarginViewModel : ViewModelBase
{
    [RelayCommand]
    private void AddCategory()
    {
        AddCategoryWindow addCategoryWindow = new AddCategoryWindow();
        AddCategoryVM addCategoryVM = new AddCategoryVM();
        addCategoryWindow.DataContext = addCategoryVM;
        addCategoryWindow.ShowDialog();
    }
}