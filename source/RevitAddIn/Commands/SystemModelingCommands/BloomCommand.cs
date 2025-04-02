using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.ViewModels;

namespace RevitAddIn.Commands.SystemModelingCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class BloomCommand : ExternalCommand
{

    public override void Execute()
    {
        var viewModel = new BloomViewModel();
    }
}