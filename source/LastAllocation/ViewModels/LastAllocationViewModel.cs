using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using Autodesk.Revit.UI;
using LastAllocation.Models;

namespace LastAllocation.ViewModels;

public sealed partial class LastAllocationViewModel : ObservableObject
{
    private readonly UIDocument _uiDoc = Context.ActiveUiDocument;
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private ObservableCollection<SelectionHistoryItem> _selectionHistoryItems = [];

    public LastAllocationViewModel(ObservableCollection<SelectionHistoryData> selectionHistories)
    {
        int index = 1;
        foreach (var history in selectionHistories)
        {
            // Фильтруем только действительные ElementId
            List<ElementId> validElementIds = GetValidElementIds(history.ElementsIds);

            // Обновляем коллекцию элементов, оставляя только действительные
            history.ElementsIds = validElementIds;

            // Добавляем в список истории только если остались действительные элементы
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
        try
        {
            // Проверяем, что параметр - это правильный объект истории выделения
            var selectionItem = parameter as SelectionHistoryItem;
            if (selectionItem == null)
                return;

            // Фильтруем только действительные ElementId для текущего документа
            ICollection<ElementId> validIds = GetValidElementIds(selectionItem.ElementIds);

            // Применяем выделение только к действительным элементам
            if (validIds.Count > 0)
            {
                _uiDoc.Selection.SetElementIds(validIds);
                _uiDoc.ShowElements(validIds);
            }
            else
            {
                // Можно показать сообщение пользователю, что элементы не найдены
                MessageBox.Show("Выбранные элементы больше не существуют в текущем документе.", "Предупреждение");
            }
        }
        catch (Exception e)
        {
           MessageBox.Show($"Не удалось выделить элементы: {e.Message}", "Ошибка");
        }
    }

    private void UpdateHistoryItems(ObservableCollection<SelectionHistoryData> histories)
    {
        // Очищаем существующие элементы
        SelectionHistoryItems.Clear();

        // Добавляем новые элементы
        int index = 1;
        foreach (var history in histories)
        {
            // Фильтруем только действительные ElementId
            var validElementIds = GetValidElementIds(history.ElementsIds);

            // Добавляем в список истории только если остались действительные элементы
            if (validElementIds.Count > 0)
            {
                // Если класс SelectionHistoryData позволяет изменять ElementsIds
                if (history.ElementsIds is { } listIds)
                {
                    listIds.Clear();
                    foreach (var id in validElementIds)
                    {
                        listIds.Add(id);
                    }

                    SelectionHistoryItems.Add(new SelectionHistoryItem(history, index));
                }
            }

            index++;
        }
    }

    private List<ElementId> GetValidElementIds(List<ElementId> elementIds)
    {
        List<ElementId> validElementIds = new List<ElementId>();

        foreach (ElementId id in elementIds)
        {
            // Проверяем, существует ли элемент в текущем документе
            Element elem = _doc.GetElement(id);
            if (elem != null)
            {
                validElementIds.Add(id);
            }
        }

        return validElementIds;
    }
}