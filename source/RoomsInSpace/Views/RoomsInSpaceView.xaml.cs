using RoomsInSpaces.ViewModels;

namespace RoomsInSpaces.Views
{
    public sealed partial class RoomsInSpaceView
    {
        public RoomsInSpaceView(RoomsInSpacesViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}