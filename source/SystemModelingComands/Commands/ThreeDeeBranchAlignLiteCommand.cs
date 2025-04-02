using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace SystemModelingCommands.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ThreeDeeBranchAlignLiteCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices systemModelingServices = new SystemModelingServices();
        systemModelingServices.ThreeDeeBranchAlignLite();
    }
}