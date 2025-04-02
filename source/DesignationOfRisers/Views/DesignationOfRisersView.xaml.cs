using DesignationOfRisers.ViewModels;

namespace DesignationOfRisers.Views
{
    public sealed partial class DesignationOfRisersView
    {
        public DesignationOfRisersView(DesignationOfRisersViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

      
    }
}