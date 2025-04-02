using Autodesk.Revit.Attributes;
using ElementsTypicalFloor.Services;
using ElementsTypicalFloor.ViewModels;
using ElementsTypicalFloor.Views;
using Nice3point.Revit.Toolkit.External;

namespace RevitAddIn.Commands.CreatingSpecificationsCommands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class ElementsTypicalFloorCommand : ExternalCommand
    {
        public override void Execute()
        {
            if (WindowController.Focus<ElementsTypicalFloorView>()) return;
            var viewModel = new ElementsTypicalFloorViewModel();
            var view = new ElementsTypicalFloorView(viewModel);
            WindowController.Show(view, UiApplication.MainWindowHandle);

        }
    }
}
