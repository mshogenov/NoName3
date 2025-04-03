using NumberingOfRisers.ViewModels;

namespace NumberingOfRisers.Views;

public partial class NumberingOfRisersView 
{
    public NumberingOfRisersView(NumberingOfRisersViewModel viewModel)
    {
        InitializeComponent();
        LoadWindowTemplate();
        DataContext = viewModel;
    }
}