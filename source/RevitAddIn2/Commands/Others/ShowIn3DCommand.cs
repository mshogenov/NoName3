using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using ShowIn3D.Services;

namespace RevitAddIn2.Commands.Others;
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ShowIn3DCommand : ExternalCommand
{
    public override void Execute()
    {
        ShowIn3DService service = new ShowIn3DService();
        service.ShowIn3D();
    }
}