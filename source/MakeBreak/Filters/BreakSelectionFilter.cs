using Autodesk.Revit.UI.Selection;

namespace MakeBreak.Filters;

public class BreakSelectionFilter : ISelectionFilter
{
    private readonly FamilySymbol _familySymbol;

    public BreakSelectionFilter(FamilySymbol familySymbol)
    {
        _familySymbol = familySymbol;
    }

    public bool AllowElement(Element elem)
    {
        return elem.Name==_familySymbol.Name || elem is DisplacementElement;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}