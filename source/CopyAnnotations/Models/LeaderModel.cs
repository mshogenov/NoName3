namespace CopyAnnotations.Models;

public class LeaderModel
{
    public Leader Leader { get; set; }
    public XYZ Anchor => Leader.Anchor;
    public XYZ Elbow => Leader.Elbow;
    public XYZ End => Leader.End;
    public LeaderShape LeaderShape => Leader.LeaderShape;

    public LeaderModel(Leader leader)
    {
        if (leader == null) return;
        Leader = leader;
    }
}