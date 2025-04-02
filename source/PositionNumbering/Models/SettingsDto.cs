namespace PositionNumbering.Models;

public class SettingsDto
{
    public string Name { get; set; }
    public bool NumberingIsChecked { get; set; }
    public List<SystemModel> Systems { get; set; }
}