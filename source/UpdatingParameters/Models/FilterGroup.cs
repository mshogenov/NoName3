using System.Collections.ObjectModel;

namespace UpdatingParameters.Models;

public partial class FilterGroup : ObservableObject
{
    // Добавляем свойство для хранения ссылки на родительскую группу
    public FilterGroup? Parent { get; private set; }
    [ObservableProperty] private ObservableCollection<object> _items = new();

    [ObservableProperty] private LogicalOperator logicalOperator = LogicalOperator.And;

    [ObservableProperty] private bool canRemove = true;

    // Добавляем массив операторов прямо в модель
    public Array LogicalOperators => Enum.GetValues(typeof(LogicalOperator));

    [RelayCommand]
    private void AddRule()
    {
        var newRule = new FilterRule { Parent = this };
        Items.Add(newRule);
    }

    [RelayCommand]
    private void Add()
    {
        var newGroup = new FilterGroup();
        newGroup.Parent = this; // Устанавливаем ссылку на родителя
        Items.Add(newGroup);
    }


    [RelayCommand]
    private void Remove(object parameter)
    {
        if (parameter is FilterGroup filterGroup && filterGroup.Parent != null)
        {
            filterGroup.Parent.Items.Remove(filterGroup);
        }
    }
}