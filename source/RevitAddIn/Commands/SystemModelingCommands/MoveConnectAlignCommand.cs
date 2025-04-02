using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace RevitAddIn.Commands.SystemModelingCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class MoveConnectAlignCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices services = new();
        services.MoveConnectAlign();
    }
}