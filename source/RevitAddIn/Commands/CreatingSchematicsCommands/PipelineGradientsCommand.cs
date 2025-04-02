using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using PipelineGradients.ViewModels;
using PipelineGradients.Views;

namespace RevitAddIn.Commands.CreatingSchematicsCommands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class PipelineGradientsCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new PipelineGradientsViewModel();
            var view = new PipelineGradientsView(viewModel);
            view.ShowDialog();
        }
    }
}
