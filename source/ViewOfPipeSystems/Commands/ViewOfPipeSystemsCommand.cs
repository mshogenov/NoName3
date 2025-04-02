using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using ViewOfPipeSystems.Services;

namespace ViewOfPipeSystems.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class ViewOfPipeSystemsCommand : ExternalCommand
    {
        public override void Execute()
        {
            ViewOfPipeSystemsServices viewOfPipeSystemsServices = new();
            viewOfPipeSystemsServices.ViewOfPipeSystems();
        }
    }
}
