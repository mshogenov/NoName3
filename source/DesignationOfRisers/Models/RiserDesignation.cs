using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace DesignationOfRisers.Models
{
    public class RiserDesignation
    {
        public PipingSystemType PipingSystemType { get; }
        public bool IsChecked {  get; set; }=false;
        public Element Mark {  get; set; }
        public RiserDesignation() 
        {
        
        
        }

    }
}
