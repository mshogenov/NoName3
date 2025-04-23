using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Autodesk.Revit.UI;
using LastAllocation.Models;

namespace LastAllocation.ViewModels;

public sealed partial class LastAllocationViewModel : ObservableObject
{
    private UIDocument _uiDoc = Context.ActiveUiDocument;
    [ObservableProperty] private ObservableCollection<SelectionHistoryItem> _selectionHistoryItems = [];
    [ObservableProperty] private SelectionHistoryItem _selectionHistory;

    public LastAllocationViewModel(ObservableCollection<SelectionHistoryData> selectionHistories)
    {
        int index = 1;
        foreach (var history in selectionHistories)
        {
            if (history.ElementsIds.Count > 0)
            {
                SelectionHistoryItems.Add(new SelectionHistoryItem(history, index));
            }

            index++;
        }

        // Подписываемся на изменения коллекции
        selectionHistories.CollectionChanged += SelectionHistories_CollectionChanged;
    }

    private void SelectionHistories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // Когда исходная коллекция изменяется, обновляем элементы модели представления
        if (sender is ObservableCollection<SelectionHistoryData> histories)
        {
            UpdateHistoryItems(histories);
        }
    }

    [RelayCommand]
    private void ApplySelection(object parameter)
    {
        _uiDoc.Selection.SetElementIds(SelectionHistory?.SelectionHistories);
        _uiDoc.ShowElements(SelectionHistory?.SelectionHistories);
    }

    private void UpdateHistoryItems(ObservableCollection<SelectionHistoryData> histories)
    {
        // Очищаем существующие элементы
        SelectionHistoryItems.Clear();

        // Добавляем новые элементы
        int index = 1;
        foreach (var history in histories)
        {
            if (history.ElementsIds.Count > 0)
            {
                SelectionHistoryItems.Add(new SelectionHistoryItem(history, index));
            }

            index++;
        }
    }
}