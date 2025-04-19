using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public sealed partial class UpdatingParametersView
{
    public UpdatingParametersView(UpdatingParametersViewModel viewModel)
    {
        InitializeComponent();
        LoadWindowTemplate();
        DataContext = viewModel;
    }
}