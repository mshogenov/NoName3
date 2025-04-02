using Autodesk.Revit.UI.Selection;

namespace NumberingOfRisers.Filters;

public class VerticalPipeSelectionFilter:ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        // Проверка на вертикальность (может быть разной логики)
        if (elem.Location is not LocationCurve location) return false;
        Curve curve = location.Curve;
        XYZ direction = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

        // Если труба вертикальная (направление по Z)
        return Math.Abs(direction.Z) > 0.9;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}