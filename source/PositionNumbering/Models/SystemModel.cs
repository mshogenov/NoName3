using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace PositionNumbering.Models;

public partial class SystemModel : ObservableObject
{
    public string Name { get; set; }
    public long SystemTypeId { get; set; }
    [JsonIgnore]
    public MEPSystemType MepSystem { get; set; } 

    [ObservableProperty] private bool _isSelected;

    public SystemModel(MEPSystemType system)
    {
        if (system == null) return;
        MepSystem = system;
        Name = system.Name;
        SystemTypeId = system.Id.Value;
    }
}