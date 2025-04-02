using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;
using NumberingOfRisers.ViewModels;
using NumberingOfRisers.Views;

namespace NumberingOfRisers.Commands;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class NumberingOfRisersCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<NumberingOfRisersView>()) return;
        var viewModel = new NumberingOfRisersViewModel();
        var view = new NumberingOfRisersView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}