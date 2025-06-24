namespace CopyAnnotations.Models;

public class CopyAnnContext
{
    private Element SelectedElement { get; set; }
    public XYZ PickPoint { get; set; }
    public XYZ DisplacementPoint { get; set; }

    public CopyAnnContext(Document doc, Reference reference)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        SelectedElement = doc.GetElement(reference);
        PickPoint = reference.GlobalPoint;
        if (SelectedElement is DisplacementElement displacement)
        {
            DisplacementPoint = displacement.GetRelativeDisplacement();
        }
    }
}