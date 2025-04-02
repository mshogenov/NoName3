using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters
{
    public class MepConnectableNewSelectionFilter : ISelectionFilter
    {
        public ElementId PreviousElementId { get; set; }
#if !REVIT2024_OR_GREATER
        public bool AllowElement(Element e) => e.Category.Id.IntegerValue == -2008013 || e.Category.Id.IntegerValue == -2008130 || e.Category.Id.IntegerValue == -2008126 || e.Category.Id.IntegerValue == -2008132 || e.Category.Id.IntegerValue == -2008128 || e.Category.Id.IntegerValue == -2008000 || e.Category.Id.IntegerValue == -2008016 || e.Category.Id.IntegerValue == -2008010 || e.Category.Id.IntegerValue == -2008020 || e.Category.Id.IntegerValue == -2008050 || e.Category.Id.IntegerValue == -2001140 || e.Category.Id.IntegerValue == -2008044 || e.Category.Id.IntegerValue == -2008055 || e.Category.Id.IntegerValue == -2008049 || e.Category.Id.IntegerValue == -2001160 || e.Category.Id.IntegerValue == -2008099 && e.Id != PreviousElementID || e.Category.Id.IntegerValue == -2008193 || e.Category.Id.IntegerValue == -2008208;

#else
 public bool AllowElement(Element e) => e.Category.Id.Value == -2008013 || e.Category.Id.Value == -2008130 || e.Category.Id.Value == -2008126 || e.Category.Id.Value == -2008132 || e.Category.Id.Value == -2008128 || e.Category.Id.Value == -2008000 || e.Category.Id.Value == -2008016 || e.Category.Id.Value == -2008010 || e.Category.Id.Value == -2008020 || e.Category.Id.Value == -2008050 || e.Category.Id.Value == -2001140 || e.Category.Id.Value == -2008044 || e.Category.Id.Value == -2008055 || e.Category.Id.Value == -2008049 || e.Category.Id.Value == -2001160 || e.Category.Id.Value == -2008099 && e.Id != PreviousElementId || e.Category.Id.Value == -2008193 || e.Category.Id.Value == -2008208;

#endif

        public bool AllowReference(Reference refer, XYZ point) => false;
    }
}
