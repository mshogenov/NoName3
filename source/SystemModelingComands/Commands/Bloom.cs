using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.ViewModels;


namespace SystemModelingCommands.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class Bloom : ExternalCommand
{
    public override void Execute()
    {
        var viewModel = new BloomViewModel();
    }
}