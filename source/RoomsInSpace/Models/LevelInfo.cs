namespace RoomsInSpaces.Models;

public class LevelInfo
{
    public Level Level { get; set; }
    public string LevelName { get; set; }
    public int RoomCount { get; set; }
    public ElementId LevelId { get; set; }
    public bool IsChecked { get; set; }
}