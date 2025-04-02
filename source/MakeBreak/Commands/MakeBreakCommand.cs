using Autodesk.Revit.Attributes;
using MakeBreak.ViewModels;
using MakeBreak.Views;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;

namespace MakeBreak.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class MakeBreakCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<MakeBreakView>()) return;
        var viewModel = new MakeBreakViewModel();
        var view = new MakeBreakView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}