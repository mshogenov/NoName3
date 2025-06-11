using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UpdatingParameters.Models;
using FilterRule = UpdatingParameters.Models.FilterRule;

namespace UpdatingParameters.ViewModels;

public partial class FilteringCriteriaVM : ObservableObject
{
  
    [ObservableProperty]
    private FilterGroup rootGroup;

    [ObservableProperty] 
    private Array condition = Enum.GetValues(typeof(Condition));

    public FilteringCriteriaVM()
    {
        RootGroup = new FilterGroup { CanRemove = false };
    }
}