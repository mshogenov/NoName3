using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters
{
    public class MepCurveSelectionFilter : ISelectionFilter
    {
        public ElementId PreviousElementId { get; set; }

        public bool AllowElement(Element e) => e.Category?.Id.Value == -2008130 || e.Category?.Id.Value == -2008132 ||
                                               e.Category?.Id.Value == -2008000 ||
                                               e.Category?.Id.Value == -2008044 && e.Id != PreviousElementId ||
                                               e.Category?.Id.Value == -2008193 || e.Category?.Id.Value == -2008208;


        public bool AllowReference(Reference r, XYZ p) => true;
    }
}