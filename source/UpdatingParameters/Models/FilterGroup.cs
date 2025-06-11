using System.Collections.ObjectModel;

namespace UpdatingParameters.Models;

public partial class FilterGroup: ObservableObject
{
    public ObservableCollection<object> Items { get; set; } = new(); // Может содержать FilterRule или FilterGroup

    [ObservableProperty]
    private EnrollmentCondition enrollmentCondition = EnrollmentCondition.And;

   
}