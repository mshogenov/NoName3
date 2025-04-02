using DeleteViewFilters.ViewModels;
using System.Windows.Controls;

namespace DeleteViewFilters.Views
{
    public sealed partial class DeleteViewFiltersView
    {
        public DeleteViewFiltersView(DeleteViewFiltersViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
       
    }
}