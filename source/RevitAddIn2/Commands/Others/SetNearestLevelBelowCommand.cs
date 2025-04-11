using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using RevitAddIn1.Services;

namespace RevitAddIn2.Commands.Others;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class SetNearestLevelBelowCommand : ExternalCommand
{
    public override void Execute()
    {
        SetNearestLevelBelowServices setNearestLevelBelowServices = new();
        setNearestLevelBelowServices.SetNearestLevelBelow();
    }
}