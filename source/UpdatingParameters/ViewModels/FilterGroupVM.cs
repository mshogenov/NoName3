using System.Collections.ObjectModel;
using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class FilterGroupVM : FilterItem
{
    public ObservableCollection<FilterItem> Children { get; set; } = new ObservableCollection<FilterItem>();
    private string _logicalOperator = "И";

    public string LogicalOperator
    {
        get => _logicalOperator;
        set
        {
            _logicalOperator = value;
            OnPropertyChanged();
        }
    }
    // Добавьте это свойство
    public bool CanBeRemoved => Parent != null;

    [RelayCommand]
    private void AddRule()
    {
        var rule = new FilterRuleVM();
        rule.Parent = this; // Устанавливаем родителя
        Children.Add(rule);
    }

    [RelayCommand]
    private void AddGroup()
    {
        var group = new FilterGroupVM();
        group.Parent = this; // Устанавливаем родителя
        Children.Add(group);
    }

    [RelayCommand]
    private void Remove()
    {
        if (Parent is FilterGroupVM parentGroup)
        {
            parentGroup.Children.Remove(this);
        }
    }
}