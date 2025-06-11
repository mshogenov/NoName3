using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class FilterGroupVM : ObservableObject
{
    private FilterGroup _filterGroup = new();

    public FilterGroup FilterGroup
    {
        get => _filterGroup;
        set
        {
            if (Equals(value, _filterGroup)) return;
            _filterGroup = value;
            OnPropertyChanged();
        }
    }


    [ObservableProperty] private Array _enrollmentCondition = Enum.GetValues(typeof(EnrollmentCondition));
    [ObservableProperty] private EnrollmentCondition _selectedEnrollmentCondition = Models.EnrollmentCondition.And;

    [RelayCommand]
    private void AddRule()
    {
        var rule = new FilterRuleVM
        {
        };
        FilterGroup.Items.Add(rule);
    }

    [RelayCommand]
    private void AddGroup()
    {
        var group = new FilterGroupVM();

        FilterGroup.Items.Add(group);
    }

    [RelayCommand]
    private void Remove()
    {
    }
}