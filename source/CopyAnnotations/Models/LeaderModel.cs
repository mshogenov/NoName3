namespace CopyAnnotations.Models;

public class LeaderModel
{
    public XYZ Anchor { get; set; }
    public XYZ Elbow { get; set; }
    public XYZ End { get; set; }
    public LeaderShape LeaderShape { get; set; }

    public LeaderModel(Leader leader)
    {
        if (leader == null) return;
        Anchor = leader.Anchor;
        Elbow = leader.Elbow;
        End = leader.End;
        LeaderShape = leader.LeaderShape;
    }
}