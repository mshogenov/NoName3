using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters
{
    public class DuctSelectionFilter : ISelectionFilter
    {
      

public bool AllowElement(Element e) => e.Category.Id.Value == -2008000 ;



        public bool AllowReference(Reference r, XYZ p) => true;
    }
}