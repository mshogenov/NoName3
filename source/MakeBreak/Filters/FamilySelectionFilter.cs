using Autodesk.Revit.UI.Selection;

namespace MakeBreak.Filters;

public class FamilySelectionFilter : ISelectionFilter
{
    private readonly FamilySymbol _familySymbol;

    public FamilySelectionFilter(FamilySymbol familySymbol)
    {
        _familySymbol = familySymbol;
    }

    public bool AllowElement(Element elem)
    {
        return elem.Name==_familySymbol.Name;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}