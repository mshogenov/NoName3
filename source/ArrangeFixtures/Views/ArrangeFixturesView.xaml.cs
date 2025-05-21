using System.Windows;
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

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
      Close();
    }
}