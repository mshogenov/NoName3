using System.Collections.ObjectModel;
using Autodesk.Revit.DB.Plumbing;
using Newtonsoft.Json;

namespace NumberingOfRisers.Models;

public partial class RiserSystemType : ObservableObject
{
    public string MepSystemTypeName { get; set; }
    [ObservableProperty] private bool _isChecked;

    [ObservableProperty] private ObservableCollection<Riser> _risers;
   

    public RiserSystemType(List<Riser> risers)
    {
        if (risers == null || risers.Count == 0)return;
        _risers = new ObservableCollection<Riser>(risers);
        MepSystemTypeName = risers.FirstOrDefault()?.MepSystemType.Name;
    }
}