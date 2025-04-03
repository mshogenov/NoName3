using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using PlacementOfStamps.ViewModels;
using PlacementOfStamps.Views;

namespace PlacementOfStamps.Commands;
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class PlacementOfStampsCommand : ExternalCommand
{
  
    public override void Execute()
    {
        var viewModel = new PlacementOfStampsViewModel();
        var view = new PlacementOfStampsView(viewModel);
        view.ShowDialog();
    }
}