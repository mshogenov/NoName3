using RevitAddIn1.ViewModels;

namespace RevitAddIn1.Views;

public sealed partial class RevitAddIn1View
{
    public RevitAddIn1View(RevitAddIn1ViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}