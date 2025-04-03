using NumberingOfRisers.Models;

namespace NumberingOfRisers.Services;

public class NumberingStrategy
{
    public string DisplayName { get; set; }
    public NumberingDirection XDirection { get; set; }
    public NumberingDirection YDirection { get; set; }
    public SortDirection PrimarySortDirection { get; private set; }

    public NumberingStrategy(string displayName, NumberingDirection xDir, NumberingDirection yDir,
        SortDirection primarySortDirection)
    {
        DisplayName = displayName;
        XDirection = xDir;
        YDirection = yDir;
        PrimarySortDirection = primarySortDirection;
    }

    public override string ToString()
    {
        return DisplayName;
    }
}