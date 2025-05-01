namespace CopyAnnotations.Models;

public class ReferenceDimensionModel
{
    public ElementReferenceType ElementReferenceType { get; set; }
    public ElementModel TaggedElement { get; set; }

    public ReferenceDimensionModel(Reference reference, Document doc)
    {
        if (reference == null) return;
        ElementReferenceType = reference.ElementReferenceType;
        TaggedElement = new ElementModel(doc.GetElement(reference.ElementId));
    }
}