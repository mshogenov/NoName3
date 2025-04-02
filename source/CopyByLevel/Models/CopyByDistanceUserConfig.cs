using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
    public class CopyByDistanceUserConfig:ObservableObject
    {
        public double Distance { get; set; } = 1000.0;

        public int CountAbove { get; set; } = 0;

        public int CountBelow { get; set; } = 0;
    }
}
