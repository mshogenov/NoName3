using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace PositionNumbering.Models;

public partial class NumberingGroupModel : ObservableObject
{
    public string Name { get; set; }

    public bool NumberingIsChecked { get; set; }

   [ObservableProperty]
    private ObservableCollection<SystemModel> _systems = [];
}