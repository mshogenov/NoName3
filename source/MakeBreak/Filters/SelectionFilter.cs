using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace MakeBreak.Filters;

public class SelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is Pipe or DisplacementElement;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}