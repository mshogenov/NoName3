using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class FilterRuleVM : ObservableObject
{
   
    [ObservableProperty] private Array _condition= Enum.GetValues(typeof(Condition));
    [ObservableProperty] private Condition _selectedCondition=Models.Condition.Equally;
  
    [RelayCommand]
    private void Remove()
    {
        // Логика удаления
    }
}