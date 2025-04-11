using Autodesk.Revit.Attributes;
using Marking.ViewModels;
using Marking.Views;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;

namespace RevitAddIn2.Commands.CreatingSchematicsCommands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class MarkingCommand : ExternalCommand
    {
        public override void Execute()
        {
            if (WindowController.Focus<MarkingView>()) return;
            var viewModel = new MarkingVM();
            var view = new MarkingView(viewModel);
            WindowController.Show(view, UiApplication.MainWindowHandle);
        }
    }
}
