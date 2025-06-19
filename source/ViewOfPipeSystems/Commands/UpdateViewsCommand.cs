using Autodesk.Revit.Attributes;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.External;
using ViewOfPipeSystems.Services;

namespace ViewOfPipeSystems.Commands
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
