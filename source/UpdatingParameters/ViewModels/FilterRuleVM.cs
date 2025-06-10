using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class FilterRuleVM : FilterItem
{
    public string Category { get; set; } = "Все выбранные категории";
    public string Parameter { get; set; } = "Имя системы";
    public string Condition { get; set; } = "содержит";
    public string Value { get; set; }


    [RelayCommand]
    private void Remove()
    {
        // Логика удаления
    }
}