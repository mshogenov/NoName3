namespace CopyAnnotations.Models;

public class TagData
{
    public ElementId TagType { get; set; }
    public IndependentTag IndependentTag { get; set; }
    public XYZ TagHeadPosition { get; set; }
    public bool HasLeader { get; set; }
    public Category TaggedElementCategory { get; set; }
    public List<Element> TaggedElements { get; set; } = [];
    public List<Reference> TaggedReferences { get; set; } = [];
    public XYZ RelativePosition { get; set; }
    public XYZ RelativeLeaderEnd { get; set; } // Позиция конца выноски относительно базовой точки
    public XYZ LeaderVector { get; set; } // Вектор направления выноски от позиции марки
    public TagOrientation Orientation { get; set; }

    public XYZ LeaderElbow { get; set; }
    public XYZ LeaderEnd { get; set; }
    public LeaderEndCondition LeaderEndCondition { get; set; }
    public XYZ RelativeLeaderElbow { get; set; }

    public TagData(IndependentTag tag)
    {
        IndependentTag = tag;
        TagHeadPosition = tag.TagHeadPosition;
        HasLeader = tag.HasLeader;
        TagType = tag.GetTypeId();
        Document doc = tag.Document;
        ICollection<LinkElementId> taggedElementIds = tag.GetTaggedElementIds();
        if (taggedElementIds is { Count: > 0 })
        {
            foreach (var taggedElementId in taggedElementIds)
            {
                TaggedElements.Add(doc.GetElement(taggedElementId.HostElementId));
            }
        }

        if (TaggedElements is { Count: > 0 })
        {
            foreach (var taggedElement in TaggedElements)
            {
                TaggedReferences.Add(new Reference(taggedElement));
            }
        }

        if (TaggedElements is { Count: > 0 })
        {
            TaggedElementCategory = TaggedElements.FirstOrDefault()?.Category;
        }

        Orientation = tag.TagOrientation;
        LeaderEnd = tag.GetLeaderEnd(TaggedReferences.FirstOrDefault());
        LeaderElbow = tag.GetLeaderElbow(TaggedReferences.FirstOrDefault());
        LeaderEndCondition = tag.LeaderEndCondition;
        LeaderVector = LeaderEnd - TagHeadPosition;
    }

    public void GetRelativePositions(XYZ position)
    {
        RelativePosition = TagHeadPosition - position;
        RelativeLeaderEnd = LeaderEnd - position;
        RelativeLeaderElbow = LeaderElbow - position;
    }
}