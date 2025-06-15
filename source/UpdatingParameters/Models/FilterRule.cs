namespace UpdatingParameters.Models;

public partial class FilterRule : ObservableObject
{
    [ObservableProperty] private Condition selectedCondition = Models.Condition.Equally;
    [ObservableProperty] private string parameter;
    [ObservableProperty] private string value;
    public FilterGroup Parent { get; set; } // Ссылка на родительскую группу
    public Array Conditions => Enum.GetValues(typeof(Condition));

    [RelayCommand]
    private void Remove()
    {
        if (Parent != null)
        {
            Parent.Items.Remove(this);
        }
    }
}