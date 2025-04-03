namespace PlacementOfStamps.Models;

public class TagModels
{
    public IndependentTag TagElement { get; set; }
    public XYZ TagPosition { get; set; }
    public double Parameter { get; set; }
    public ElementId TagTypeId { get; set; }
    public string Name { get; set; }
    public BoundingBoxXYZ BoundingBox { get; set; }
    public double Distance { get; set; }
    public ICollection<Element> TaggedLocalElements { get; set; } = [];

    public TagModels(IndependentTag tag)
    {
        TagElement = tag;
        Name = tag.Name;
        TagPosition = tag.TagHeadPosition;
        TagTypeId = tag.GetTypeId();
        foreach (var taggedLocalElement in tag.GetTaggedLocalElements())
        {
            TaggedLocalElements.Add(taggedLocalElement);
        }

    }
}