using CopyByLevel.ViewModels;

namespace CopyByLevel.Views
{
    public sealed partial class CopyByLevelView
    {
        public CopyByLevelView(CopyByLevelViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}