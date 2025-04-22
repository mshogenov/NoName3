using System.Windows;
using Autodesk.Revit.UI;

namespace RevitAddIn2.Commands.Others;

public partial class SelectionHistoryWindow : Window
{
    private readonly UIDocument _uidoc;

    public SelectionHistoryWindow(UIDocument uidoc)
    {
        InitializeComponent();
        _uidoc = uidoc;
        LoadSelectionHistories();
    }

    private void LoadSelectionHistories()
    {
        int index = 1;
        foreach (var history in RevitAddIn2.Application.SelectionHistories)
        {
            if (history.Count > 0)
            {
                ListBoxHistories.Items.Add(new SelectionHistoryItem
                {
                    Index = index,
                    ElementCount = history.Count,
                    SelectionHistory = history
                });
            }

            index++;
        }
    }

    private void ButtonApply_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = ListBoxHistories.SelectedItem as SelectionHistoryItem;
        if (selectedItem != null)
        {
            _uidoc.Selection.SetElementIds(selectedItem.SelectionHistory);
        }

        Close();
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class SelectionHistoryItem
{
    public int Index { get; set; }
    public int ElementCount { get; set; }
    public List<ElementId> SelectionHistory { get; set; }

    public override string ToString()
    {
        return $"Выделение #{Index} - {ElementCount} элемент(ов)";
    }
}