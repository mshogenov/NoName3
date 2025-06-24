namespace PlacementOfStamps.Models;

public class ElementWrp
{
    public ElementId Id { get; set; }
    public Reference Reference { get; set; }
    public BuiltInCategory Category { get; set; }
    public Element Element { get; set; }
    public XYZ Position { get; set; }

    public ElementWrp(Element element)
    {
        if (element == null) return;
        Element = element;
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
        switch (location)
        {
            case LocationPoint locationPoint:
                return locationPoint.Point;
            case LocationCurve locationCurve:
                // Вариант 1: используя Evaluate
                return (locationCurve.Curve as Line)?.Evaluate(0.5, true);
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