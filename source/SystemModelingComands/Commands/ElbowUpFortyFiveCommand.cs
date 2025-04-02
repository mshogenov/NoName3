using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using SystemModelingCommands.Services;

namespace SystemModelingCommands.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class ElbowUpFortyFiveCommand : ExternalCommand
    {
        public override void Execute()
        {
            SystemModelingServices services = new();
            services.ElbowUpFortyFive();
        }
    }
}
