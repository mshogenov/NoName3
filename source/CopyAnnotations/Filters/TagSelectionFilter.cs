using Autodesk.Revit.UI.Selection;

namespace CopyAnnotations.Filters;

public class TagSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is IndependentTag or TextNote;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}