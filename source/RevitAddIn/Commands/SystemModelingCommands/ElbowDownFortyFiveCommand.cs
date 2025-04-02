using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace RevitAddIn.Commands.SystemModelingCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ElbowDownFortyFiveCommand : ExternalCommand
{
    public override void Execute()
    {
        SystemModelingServices systemModelingServices = new SystemModelingServices();
        systemModelingServices.ElbowDownFortyFive();
    }
}