namespace LastAllocation.Models;

public class SelectionHistoryData
{
    public List<ElementId> ElementsIds { get; set; }
    public DateTime Time { get; set; }

    public SelectionHistoryData(List<ElementId> elementsIds)
    {
        ElementsIds = elementsIds;
        Time = DateTime.Now;
    }
}