using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using UpdatingParameters.ViewModels;
using UpdatingParameters.Views;

namespace RevitAddIn.Commands.CreatingSpecificationsCommands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class UpdatingParametersCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new UpdatingParametersViewModel();
            var view = new UpdatingParametersView(viewModel);
            view.ShowDialog();
        }
    }
}
