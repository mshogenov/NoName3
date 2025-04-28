namespace CopyAnnotations.Models;

public class LeaderElbowModel
{
    public XYZ Position { get; set; }
    public ElementModel TaggedElement { get; set; }
    public LeaderElbowModel(IndependentTag tag, ElementModel element)
    {
        TaggedElement = element;
        Position = tag.GetLeaderElbow(element.Reference);
    }
}