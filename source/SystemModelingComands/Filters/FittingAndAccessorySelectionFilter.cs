using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters;

public class FittingAndAccessorySelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element e)
    {
        if (e is not FamilyInstance instance) return false;
        return instance.MEPModel.ConnectorManager != null &&
               instance.Category.BuiltInCategory is BuiltInCategory.OST_PipeAccessory
                   or BuiltInCategory.OST_PipeFitting
                   or BuiltInCategory.OST_DuctFitting
                   or BuiltInCategory.OST_DuctAccessory;
    }

    public bool AllowReference(Reference refer, XYZ point) => false;
}