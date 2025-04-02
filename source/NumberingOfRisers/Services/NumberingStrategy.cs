using NumberingOfRisers.Models;

namespace NumberingOfRisers.Services;

public class NumberingStrategy
{
    public string DisplayName { get; set; }
    public NumberingDirection XDirection { get; set; }
    public NumberingDirection YDirection { get; set; }

    public NumberingStrategy(string displayName, NumberingDirection xDir, NumberingDirection yDir)
    {
        DisplayName = displayName;
        XDirection = xDir;
        YDirection = yDir;
    }

    public override string ToString()
    {
        return DisplayName;
    }
}