using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;


namespace SystemModelingCommands.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class InsertPipeCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices systemModelingServices = new();
        systemModelingServices.InsertPipe();
    }
}