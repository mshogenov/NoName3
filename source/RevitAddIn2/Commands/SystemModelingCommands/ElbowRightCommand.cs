using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace RevitAddIn2.Commands.SystemModelingCommands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ElbowRightCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices systemModelingServices = new SystemModelingServices();
        systemModelingServices.ElbowRight();
    }
}