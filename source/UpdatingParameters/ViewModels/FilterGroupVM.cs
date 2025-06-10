using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class FilterGroupVM : FilterItem
{
    private ObservableCollection<FilterItem> _children = new();

    public ObservableCollection<FilterItem> Children
    {
        get => _children;
        set
        {
            if (_children != null)
            {
                _children.CollectionChanged -= OnChildrenChanged;
            }

            _children = value;

            if (_children != null)
            {
                _children.CollectionChanged += OnChildrenChanged;
                // Устанавливаем Parent для существующих элементов
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
            }

            OnPropertyChanged();
        }
    }

    private void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // Устанавливаем Parent для новых элементов
        if (e.NewItems != null)
        {
            foreach (FilterItem item in e.NewItems)
            {
                item.Parent = this;
            }
        }

        // Очищаем Parent для удаленных элементов
        if (e.OldItems != null)
        {
            foreach (FilterItem item in e.OldItems)
            {
                item.Parent = null;
            }
        }
    }

    [ObservableProperty] private Array _enrollmentCondition = Enum.GetValues(typeof(EnrollmentCondition));
    [ObservableProperty] private EnrollmentCondition _selectedEnrollmentCondition = Models.EnrollmentCondition.And;
    public bool CanBeRemoved => Parent != null;

    [RelayCommand]
    private void AddRule()
    {
        var rule = new FilterRuleVM
        {
            Parent = this // Устанавливаем родителя
        };
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