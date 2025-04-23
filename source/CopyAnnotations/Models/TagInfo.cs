namespace CopyAnnotations.Models;

public class TagInfo
{
    public ElementId CategoryId { get; set; }
    public bool HasLeader { get; set; }
    public XYZ LeaderEnd { get; set; }
    public XYZ TagHeadPosition { get; set; }
    public ElementId TagType { get; set; }
    public TagOrientation TagOrientation { get; set; }
}