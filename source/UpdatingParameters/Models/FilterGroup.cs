using System.Collections.ObjectModel;

namespace UpdatingParameters.Models;

public partial class FilterGroup: ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<object> items = new();

    [ObservableProperty]
    private LogicalOperator logicalOperator = LogicalOperator.And;

    [ObservableProperty]
    private bool canRemove = true;

    // Добавляем массив операторов прямо в модель
    public Array LogicalOperators => Enum.GetValues(typeof(LogicalOperator));

    [RelayCommand]
    private void AddRule()
    {
        Items.Add(new FilterRule());
    }

    [RelayCommand]
    private void AddGroup()
    {
        Items.Add(new FilterGroup());
    }

    [RelayCommand]
    private void Remove()
    {
        // Логика удаления будет обрабатываться родительским элементом
    }
}