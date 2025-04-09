namespace RevitAddIn1.Models;

public class TagData
{
    public ElementId TagType { get; set; }
    public ElementId TaggedElementCategory { get; set; }
    public XYZ RelativePosition { get; set; }
    public XYZ RelativeLeaderEnd { get; set; } // Позиция конца выноски относительно базовой точки
    public XYZ LeaderVector { get; set; } // Вектор направления выноски от позиции марки
    public TagOrientation Orientation { get; set; }
    public bool Leader { get; set; }
    public XYZ LeaderElbow { get; set; }
    public LeaderEndCondition LeaderEndCondition { get; set; }
    public XYZ RelativeLeaderElbow { get; set; }
}