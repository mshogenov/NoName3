using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using ViewOfPipeSystems.Services;

namespace RevitAddIn.Commands.CreatingSchematicsCommands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class UpdateViewsCommand : ExternalCommand
    {
        public override void Execute()
        {
            ViewOfPipeSystemsServices viewOfPipeSystemsServices = new();
            viewOfPipeSystemsServices.UpdateViews();
        }
    }
}
