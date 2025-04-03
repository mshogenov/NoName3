using PlacementOfStamps.ViewModels;

namespace PlacementOfStamps.Views;

public sealed partial class PlacementOfStampsView
{
    public PlacementOfStampsView(PlacementOfStampsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}