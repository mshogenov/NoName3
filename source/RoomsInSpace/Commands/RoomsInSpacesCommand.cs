using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using RoomsInSpaces.ViewModels;
using RoomsInSpaces.Views;

namespace RoomsInSpaces.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class RoomsInSpacesCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new RoomsInSpacesViewModel();
            var view = new RoomsInSpaceView(viewModel);
            view.ShowDialog();
        }
    }
}
