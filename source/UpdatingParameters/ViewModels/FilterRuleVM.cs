using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class FilterRuleVM : FilterItem
{
    public string Category { get; set; } 
    public string Parameter { get; set; } 
    [ObservableProperty] private Array _condition= Enum.GetValues(typeof(Condition));
    [ObservableProperty] private Condition _selectedCondition=Models.Condition.Equally;
    public string Value { get; set; }


    [RelayCommand]
    private void Remove()
    {
        // Логика удаления
    }
}