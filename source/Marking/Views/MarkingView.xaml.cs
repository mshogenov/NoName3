using Marking.ViewModels;

namespace Marking.Views;

public sealed partial class MarkingView
{

    public MarkingView(MarkingVM viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        LoadWindowTemplate();
    }
}