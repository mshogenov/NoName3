using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters
{
    public class DuctSelectionFilter : ISelectionFilter
    {
        public ElementId PreviousElementId { get; set; }
#if !REVIT2024_OR_GREATER
        public bool AllowElement(Element e) => e.Category.Id.IntegerValue == -2008000 && e.Id != PreviousElementID;
#else
public bool AllowElement(Element e) => e.Category.Id.Value == -2008000 && e.Id != PreviousElementId;
#endif


        public bool AllowReference(Reference r, XYZ p) => true;
    }
}