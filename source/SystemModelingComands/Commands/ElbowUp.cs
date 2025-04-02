using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace SystemModelingCommands.Commands;
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ElbowUpCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices systemModelingServices = new SystemModelingServices();
        systemModelingServices.ElbowUp();
    }
}