namespace PlacementOfStamps.Models;

public class LeaderElbowModel
{
    public XYZ Position { get; set; }
    public ElementWrapper TaggedElement { get; set; }

    public LeaderElbowModel(IndependentTag tag, ElementWrapper element)
    {
        TaggedElement = element;
        // if (tag.LeaderEndCondition == LeaderEndCondition.Free)
        // {
        //     Position = tag.GetLeaderElbow(element.Reference);
        // }
    }
}