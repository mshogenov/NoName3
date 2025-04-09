using Autodesk.Revit.UI.Selection;

namespace RevitAddIn1.Filters;

public class TagSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is IndependentTag;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}