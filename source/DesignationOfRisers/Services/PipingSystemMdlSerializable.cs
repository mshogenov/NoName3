using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignationOfRisers.Services
{
    public class PipingSystemMdlSerializable
    {
        public string Name { get; set; }
        public string PipingSystemId { get; set; }
        public bool IsChecked { get; set; }
        public List<string> MarkIds { get; set; }
        public string SelectedMarkId { get; set; }
    }
}
