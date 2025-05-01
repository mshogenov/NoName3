namespace CopyAnnotations.Models;

public class DimensionSegmentModel
{
    public XYZ TextPosition { get; set; }
    public XYZ Origin { get; set; }
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public XYZ LeaderEndPosition { get; set; }
    public bool IsLocked { get; set; }
    public string Above { get; set; }
    public string Below { get; set; }

    public DimensionSegmentModel(DimensionSegment segment)
    {
        if (segment == null) return;
        TextPosition = segment.TextPosition;
        Origin = segment.Origin;
        Prefix = segment.Prefix;
        Suffix = segment.Suffix;
        LeaderEndPosition = segment.LeaderEndPosition;
        IsLocked = segment.IsLocked;
        Above = segment.Above;
        Below = segment.Below;
    }
}