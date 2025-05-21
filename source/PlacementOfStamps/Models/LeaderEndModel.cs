namespace PlacementOfStamps.Models;

public class LeaderEndModel
{
    public XYZ Position { get; set; }
    public ElementWrapper TaggedElement { get; set; }

    public LeaderEndModel(IndependentTag tag, ElementWrapper element)
    {
        TaggedElement = element;
        // Проверяем условия для получения LeaderEnd
        if (tag.LeaderEndCondition == LeaderEndCondition.Free)
        {
            Position = tag.GetLeaderEnd(element.Reference);
        }
    }
}