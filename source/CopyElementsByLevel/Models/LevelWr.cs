using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyElementsByLevel.Models
{
   public partial class LevelWr : ObservableObject
    {
        public ElementId Id { get; }

        public string Name { get; }

        public double Elevation { get; }

        public string Title { get; }
        [ObservableProperty] private bool _isSelected;


        public LevelWr(Level level)
        {
           Id = level.Id;
           Elevation = level.Elevation;
           Title = level.Name;
           Name = string.Format("{0} ({1}{2:0.000})", level.Name, Elevation > 0.0 ? "+" : (object)string.Empty, Elevation.ToMillimeters() / 1000.0);
        }

        
    }
}
