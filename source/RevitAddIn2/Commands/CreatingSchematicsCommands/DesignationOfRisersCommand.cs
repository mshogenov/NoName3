using Autodesk.Revit.Attributes;
using DesignationOfRisers.ViewModels;
using DesignationOfRisers.Views;
using Nice3point.Revit.Toolkit.External;

namespace RevitAddIn2.Commands.CreatingSchematicsCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class DesignationOfRisersCommand : ExternalCommand
{
    public override void Execute()
    {
        var viewModel = new DesignationOfRisersViewModel();
        var view = new DesignationOfRisersView(viewModel);
        view.ShowDialog();
    }
}