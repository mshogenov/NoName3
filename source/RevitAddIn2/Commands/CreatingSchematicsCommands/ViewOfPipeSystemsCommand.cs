using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using ViewOfPipeSystems.Services;
using ViewOfPipeSystems.ViewModels;
using ViewOfPipeSystems.Views;

namespace RevitAddIn2.Commands.CreatingSchematicsCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ViewOfPipeSystemsCommand : ExternalCommand
{
    public override void Execute()
    {
        var viewModel = new ViewOfPipeSystemsVM();
        var view = new ViewOfPipeSystemWindow(viewModel);
        view.ShowDialog();
    }
}