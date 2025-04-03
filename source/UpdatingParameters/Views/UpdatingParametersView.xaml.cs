using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public sealed partial class UpdatingParametersView
{
    public UpdatingParametersView(UpdatingParametersViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
       
    }
}