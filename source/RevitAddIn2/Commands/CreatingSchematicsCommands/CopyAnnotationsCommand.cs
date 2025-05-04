using Autodesk.Revit.Attributes;
using CopyAnnotations.Services;
using CopyAnnotations.ViewModels;
using CopyAnnotations.Views;
using Nice3point.Revit.Toolkit.External;
using NoNameApi.Services;

namespace RevitAddIn2.Commands.CreatingSchematicsCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class CopyAnnotationsCommand : ExternalCommand
{
    public override void Execute()
    {
        if (WindowController.Focus<CopyAnnotationsView>()) return;
        CopyAnnotationsViewModel viewModel = new();
        var view = new CopyAnnotationsView(viewModel);
        WindowController.Show(view, UiApplication.MainWindowHandle);
    }
}