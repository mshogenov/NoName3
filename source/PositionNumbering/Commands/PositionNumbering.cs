using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using PositionNumbering.ViewModels;
using PositionNumbering.Views;

namespace PositionNumbering.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class PositionNumbering : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new NumberingViewModel();
            var view = new PositionNumberingWindow(viewModel);
            view.ShowDialog();
        }
    }
}
