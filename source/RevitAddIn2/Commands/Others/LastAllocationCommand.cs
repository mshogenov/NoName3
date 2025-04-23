using Autodesk.Revit.Attributes;
using LastAllocation.ViewModels;
using LastAllocation.Views;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;

namespace RevitAddIn2.Commands.Others;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class LastAllocationCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<LastAllocationView>()) return;
        var viewModel = new LastAllocationViewModel(RevitAddIn2.Application.SelectionHistories);
        var view = new LastAllocationView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}