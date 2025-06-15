using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public partial class SelectedCategoriesControl
{
    public SelectedCategoriesControl()
    {
        InitializeComponent();
        DataContext = new CategorySelectorViewModel();
    }
}