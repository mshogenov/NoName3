using Autodesk.Revit.Attributes;
using DeleteViewFilters.ViewModels;
using DeleteViewFilters.Views;
using Nice3point.Revit.Toolkit.External;

namespace DeleteViewFilters.Commands
{
    /// <summary>
    ///     External command entry point invoked from the Revit interface
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class DeleteViewFiltersCommand : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new DeleteViewFiltersViewModel();
            var view = new DeleteViewFiltersView(viewModel);
            view.ShowDialog();
        }
    }
}