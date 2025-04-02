using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using PositionNumbering.ViewModels;
using PositionNumbering.Views;

namespace RevitAddIn.Commands.CreatingSpecificationsCommands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class PositionNumberingCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new NumberingViewModel();
            var view = new PositionNumberingWindow(viewModel);
            view.ShowDialog();
        }
    }
}
