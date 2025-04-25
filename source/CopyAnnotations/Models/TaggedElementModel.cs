namespace CopyAnnotations.Models;

public class TaggedElementModel
{
    public ElementId Id { get; set; }
    public Reference Reference { get; set; }
    public BuiltInCategory Category { get; set; }
    public Element Element { get; set; }

    public TaggedElementModel(Element element)
    {
        Element = element;
        if (element == null) return;
        Id = element.Id;
        Reference = new Reference(element);
        Category = (BuiltInCategory)element.Category.Id.Value;
    }
}