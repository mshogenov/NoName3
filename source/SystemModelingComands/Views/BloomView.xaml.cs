


using SystemModelingCommands.ViewModels;

namespace SystemModelingCommands.Views
{
    public sealed partial class BloomView
    {
        public BloomView(BloomViewModel viewModel)
        {
            
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}