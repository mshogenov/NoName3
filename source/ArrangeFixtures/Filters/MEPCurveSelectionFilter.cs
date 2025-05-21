using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace ArrangeFixtures.Filters;

public class MEPCurveSelectionFilter:ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is Pipe;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}