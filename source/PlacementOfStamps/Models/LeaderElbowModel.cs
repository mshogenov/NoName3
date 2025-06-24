namespace PlacementOfStamps.Models;

public class LeaderElbowModel
{
    public XYZ Position { get; set; }
    public ElementWrp TaggedElement { get; set; }

    public LeaderElbowModel(IndependentTag tag, ElementWrp element)
    {
        TaggedElement = element;
        // if (tag.LeaderEndCondition == LeaderEndCondition.Free)
        // {
        //     Position = tag.GetLeaderElbow(element.Reference);
        // }
    }
}