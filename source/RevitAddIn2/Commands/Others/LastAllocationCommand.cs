using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace RevitAddIn2.Commands.Others;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class LastAllocationCommand : ExternalCommand
{
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;

    public override void Execute()
    {
        // Вместо немедленного применения выделения показываем диалог выбора
        ShowSelectionHistoryDialog();
    }

    private void ShowSelectionHistoryDialog()
    {
        var dialog = new SelectionHistoryWindow(_uidoc);
        dialog.ShowDialog();
    }
}