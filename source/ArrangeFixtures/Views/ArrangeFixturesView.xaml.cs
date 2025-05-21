using ArrangeFixtures.ViewModels;

namespace ArrangeFixtures.Views;

public sealed partial class ArrangeFixturesView
{
    public ArrangeFixturesView(ArrangeFixturesViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        LoadWindowTemplate();
    }
}