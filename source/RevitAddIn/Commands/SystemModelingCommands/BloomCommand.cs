using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace RevitAddIn.Commands.SystemModelingCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class BloomCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices systemModelingServices = new();
        systemModelingServices.Bloom();
    }
}