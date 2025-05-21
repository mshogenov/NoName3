using ArrangeFixtures.ViewModels;
using ArrangeFixtures.Views;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;

namespace ArrangeFixtures.Commands;
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ArrangeFixturesCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<ArrangeFixturesView>()) return;
        var viewModel = new ArrangeFixturesViewModel();
        var view = new ArrangeFixturesView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}