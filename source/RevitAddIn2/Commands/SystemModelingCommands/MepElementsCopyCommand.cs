using Autodesk.Revit.Attributes;
using MepElementsCopy.ViewModels;
using MepElementsCopy.Views;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;

namespace RevitAddIn2.Commands.SystemModelingCommands;
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]

public class MepElementsCopyCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<MepElementsCopyView>()) return;
        var viewModel = new MepElementsCopyLevelsViewModel();
        var view = new MepElementsCopyView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}