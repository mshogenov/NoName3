namespace CopyAnnotations.Models;

public class TagData
{
    public ElementId Id { get; set; }
    public ElementId TagTypeId { get; set; }
    public List<ElementModel> TaggedElements { get; set; } = [];
    public XYZ TagHeadPosition { get; set; }
    public bool HasLeader { get; set; }
  public TagOrientation Orientation { get; set; }
    public XYZ LeaderElbow { get; set; }
    public XYZ LeaderEnd { get; set; }
    public BuiltInCategory TagCategory { get; set; }
    public LeaderEndCondition LeaderEndCondition { get; set; }

    public TagData(IndependentTag tag)
    {
        Id = tag.Id;
        TagHeadPosition = tag.TagHeadPosition;
        HasLeader = tag.HasLeader;
        TagTypeId = tag.GetTypeId();
        Document doc = tag.Document;
        ICollection<LinkElementId> taggedElementIds = tag.GetTaggedElementIds();
        if (taggedElementIds is { Count: > 0 })
        {
            foreach (var taggedElementId in taggedElementIds)
            {
                TaggedElements.Add(new ElementModel(doc.GetElement(taggedElementId.HostElementId)));
            }
        }

        if (TaggedElements is { Count: > 0 })
        {
            LeaderEnd = tag.GetLeaderEnd(TaggedElements.FirstOrDefault()?.Reference);
            LeaderElbow = tag.GetLeaderElbow(TaggedElements.FirstOrDefault()?.Reference);
            TagCategory = (BuiltInCategory)TaggedElements.FirstOrDefault()?.Category;
        }

        Orientation = tag.TagOrientation;
        LeaderEndCondition = tag.LeaderEndCondition;
    }
}