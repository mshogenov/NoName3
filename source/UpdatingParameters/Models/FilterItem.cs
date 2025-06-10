namespace UpdatingParameters.Models;

public abstract class FilterItem : ObservableObject
{
    public FilterItem Parent { get; set; }
}