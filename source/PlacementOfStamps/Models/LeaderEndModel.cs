namespace PlacementOfStamps.Models;

public class LeaderEndModel
{
    public XYZ Position { get; set; }
    public ElementWrp TaggedElement { get; set; }

    public LeaderEndModel(IndependentTag tag, ElementWrp element)
    {
        TaggedElement = element;
        // Проверяем условия для получения LeaderEnd
        if (tag.LeaderEndCondition == LeaderEndCondition.Free)
        {
            Position = tag.GetLeaderEnd(element.Reference);
        }
    }
}