using PipelineGradients.ViewModels;

namespace PipelineGradients.Views
{
    public sealed partial class PipelineGradientsView
    {
        public PipelineGradientsView(PipelineGradientsViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}