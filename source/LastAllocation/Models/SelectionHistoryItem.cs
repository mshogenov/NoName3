namespace LastAllocation.Models;

public class SelectionHistoryItem
{
    public int Index { get; set; }
    public int ElementCount { get; set; }
    public List<ElementId> SelectionHistories { get; set; }
    public string Name { get; set; }
    public string Time { get; set; }
    public DateTime SelectionTime { get; set; }

    public SelectionHistoryItem(SelectionHistoryData selectionHistoryData, int index)
    {
        Index = index;
        ElementCount = selectionHistoryData.ElementsIds.Count;
        SelectionHistories = selectionHistoryData.ElementsIds;
        SelectionTime = selectionHistoryData.Time; // Записываем текущее время
        UpdateName(); // Обновляем имя с учетом времени
    }

    private void UpdateName()
    {
        Time =$"[{SelectionTime:HH:mm:ss}]";
        Name = $"Выделение #{Index} - {ElementCount} элемент(ов)";
    }
}