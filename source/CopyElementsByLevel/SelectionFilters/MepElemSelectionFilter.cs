using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CopyElementsByLevel.SelectionFilters
{
    public class MepElemSelectionFilter : ISelectionFilter
    {
    
    public bool AllowElement(Element elem)
    {
        switch (elem)
        {
            case FamilyInstance familyInstance:
                if (familyInstance.MEPModel != null)
                    return !familyInstance.Symbol.Family.IsInPlace;
                break;
            case MEPCurve _:
                return true;
        }
        return false;
    }

    public bool AllowReference(Reference reference, XYZ position) => false;
}
}
