using Autodesk.Revit.UI.Selection;

namespace CopyByLevel.Models
{
    public class MepElemSelectionFilter : ISelectionFilter
    {
        private readonly IEnumerable<BuiltInCategory> _notAllowedCategories;

        public bool AllowElement(Element elem)
        {
            switch (elem)
            {
                case FamilyInstance familyInstance:
                    if (familyInstance.MEPModel != null)
                    {
                        bool isNotInPlace = !familyInstance.Symbol.Family.IsInPlace;
                        bool isInNotAllowedCategories = _notAllowedCategories.Contains((BuiltInCategory)elem.Category.Id.Value);
                        return isNotInPlace && !isInNotAllowedCategories;
                    }
                    break;
                
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;

        
    }
}