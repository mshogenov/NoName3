using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace RevitAddIn.Commands.SystemModelingCommands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class LastAllocationCommand : ExternalCommand
{
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private List<ElementId> _selectionHistory = [];

    public override void Execute()
    {
        _selectionHistory = RevitAddIn.Application.SelectionHistory;
        List<ElementId> selection = [];
        if (_selectionHistory.Count <= 0) return;
        selection.AddRange(_selectionHistory.Where(sElementId => sElementId != null));
        if (selection.Count > 0)
        {
            _uidoc.Selection.SetElementIds(_selectionHistory);
        }
    }
}