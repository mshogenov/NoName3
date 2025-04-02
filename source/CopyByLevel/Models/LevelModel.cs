using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
   public partial class LevelModel:ObservableObject
    {
        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _IsSelected;

    }
}
