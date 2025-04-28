namespace CopyAnnotations.Models;

public class LeaderEndModel
{
    public XYZ Position { get; set; }
    public ElementModel TaggedElement { get; set; }
    public LeaderEndModel(IndependentTag tag, ElementModel element)
    {
        TaggedElement = element;
        Position = tag.GetLeaderEnd(element.Reference);
    }
}