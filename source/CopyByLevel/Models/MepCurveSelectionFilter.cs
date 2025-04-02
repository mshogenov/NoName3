using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
    internal class MepCurveSelectionFilter:ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is MEPCurve mepCurve && !(mepCurve is FlexDuct) && !(mepCurve is FlexPipe);

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
