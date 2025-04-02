using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
    public class LevelWr :ObservableObject
    {
        private bool _isSelected;

        public LevelWr(Level level)
        {
            this.Id = level.Id;
            this.Elevation = level.Elevation;
            this.Title = level.Name;
            this.Name = string.Format("{0} ({1}{2:0.000})", level.Name, this.Elevation > 0.0 ? "+" : (object)string.Empty, this.Elevation.ToMillimeters() / 1000.0);
        }

        public ElementId Id { get; }

        public string Name { get; }

        public bool IsSelected
        {
            get => this._isSelected;
            set
            {
                if (value == this._isSelected)
                    return;
                this._isSelected = value;
                this.OnPropertyChanged(nameof(IsSelected));
            }
        }

        public double Elevation { get; }

        public string Title { get; }
    }
}
