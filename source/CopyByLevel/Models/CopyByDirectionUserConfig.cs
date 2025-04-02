using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
   public class CopyByDirectionUserConfig : ObservableObject
    {
        public double Distance { get; set; } = 1000.0;

        public int Count { get; set; } = 1;
    }
}
