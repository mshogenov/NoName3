using Autodesk.Revit.Attributes;
using CopyAnnotations.Services;
using Nice3point.Revit.Toolkit.External;

namespace RevitAddIn2.Commands.CreatingSchematicsCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class CopyAnnotationsCommand : ExternalCommand
{
    public override void Execute()
    {
        CopyAnnotationsServices copyAnnotationsServices = new();
        copyAnnotationsServices.CopyAnnotations();
    }
}