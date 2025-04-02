using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters
{
    public class FittingSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            if (e is not FamilyInstance instance) return false;
            return instance.MEPModel.ConnectorManager != null;
        }

        public bool AllowReference(Reference refer, XYZ point) => false;
    }
}
