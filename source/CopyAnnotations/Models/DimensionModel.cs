namespace CopyAnnotations.Models;

public class DimensionModel
{
    public Dimension Dimension { get; set; }
    public ElementId Id { get; set; }
    public DimensionType DimensionType { get; set; }
    public bool HasLeader { get; set; }
    public List<DimensionSegmentModel> Segments { get; set; } = [];
    public List<ReferenceDimensionModel> References { get; set; } = [];

    public DimensionModel(Dimension dimension)
    {
        if (dimension == null) return;
        Dimension = dimension;
        Id = dimension.Id;
        Document doc = dimension.Document;
        DimensionType = dimension.DimensionType;
        HasLeader = dimension.HasLeader;
        foreach (DimensionSegment segment in dimension.Segments)
        {
            Segments.Add(new DimensionSegmentModel(segment));
        }

        foreach (Reference reference in dimension.References)
        {
            References.Add(new ReferenceDimensionModel(reference, doc));
        }
    }
}