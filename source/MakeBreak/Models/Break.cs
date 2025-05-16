using Autodesk.Revit.DB.Plumbing;

namespace MakeBreak.Models;

public class Break
{
    public XYZ PickPoint { get; set; }
    public Element SelectedElement { get; set; }
    public PipeWrapper TargetPipe { get; set; }
    public XYZ BreakPoint { get; set; }

    public Break(Reference selectReference, Document document)
    {
        if (selectReference == null) return;
        PickPoint = selectReference.GlobalPoint;
        SelectedElement = document.GetElement(selectReference);
        TargetPipe = GetOriginalPipe(SelectedElement, PickPoint, out var primaryDisplacement);
        BreakPoint = TargetPipe.ProjectPointOntoCurve(PickPoint, primaryDisplacement);
    }

    private PipeWrapper GetOriginalPipe(Element selectedElement, XYZ pick, out DisplacementElement primaryDisplacement)
    {
        Document doc = selectedElement.Document;
        PipeWrapper originalPipe = null;
        primaryDisplacement = null;
        switch (selectedElement)
        {
            case Pipe pipe:
                originalPipe = new PipeWrapper(pipe);
                break;
            case DisplacementElement displacementElement:
            {
                primaryDisplacement = displacementElement;
                var displacementElementIds = displacementElement.GetDisplacedElementIds();

                foreach (ElementId displacedId in displacementElementIds)
                {
                    Element element = doc.GetElement(displacedId);

                    // Проверяем, является ли элемент трубой
                    if (element is not Pipe pipe) continue;
                    // Получаем геометрию трубы
                    BoundingBoxXYZ bounding = pipe.get_BoundingBox(doc.ActiveView);
                    var contains = bounding.Contains(pick);
                    if (!contains) continue;
                    // Нашли трубу, которая проходит через точку
                    originalPipe = new PipeWrapper(pipe)
                    {
                        IsDisplacement = true
                    };
                    break;
                }

                break;
            }
        }

        return originalPipe;
    }
}