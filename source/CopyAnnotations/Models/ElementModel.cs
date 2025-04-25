namespace CopyAnnotations.Models;

public class ElementModel
{
    public ElementId Id { get; set; }
    public Reference Reference { get; set; }
    public BuiltInCategory Category { get; set; }
    public Element Element { get; set; }
    public XYZ Position { get; set; }

    public ElementModel(Element element)
    {
        Element = element;
        if (element == null) return;
        Id = element.Id;
        Reference = new Reference(element);
        Category = (BuiltInCategory)element.Category.Id.Value;
        Position = GetElementPosition(element);
    }
    private XYZ GetElementPosition(Element element)
    {
        if (element == null)
            return null;

        // Пробуем получить точку расположения
        Location location = element.Location;
        if (location is LocationPoint locationPoint)
        {
            return locationPoint.Point;
        }
        else if (location is LocationCurve locationCurve)
        {
            return (locationCurve.Curve as Line)?.Origin;
        }

        // Если точки расположения нет, используем центр ограничивающего бокса
        BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
        if (boundingBox != null)
        {
            return (boundingBox.Min + boundingBox.Max) * 0.5;
        }

        return null;
    }
}