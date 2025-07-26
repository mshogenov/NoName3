using Autodesk.Revit.DB.Plumbing;

namespace MakeBreak.Models;

public class Break
{
    public Element SelectedElement { get; set; }
    public PipeWrp TargetPipe { get; set; }
    public XYZ BreakPoint { get; set; }
    public DisplacementElement PrimaryDisplacement { get; set; }

    public Break(Reference selectReference, Document document)
    {
        if (selectReference == null) return;
        var pickPoint = selectReference.GlobalPoint;
        SelectedElement = document.GetElement(selectReference);
        TargetPipe = GetOriginalPipe(SelectedElement, pickPoint, out var primaryDisplacement);
        if (primaryDisplacement != null)
        {
            PrimaryDisplacement = primaryDisplacement;
        }
        BreakPoint = TargetPipe.ProjectPointOntoCurve(pickPoint, primaryDisplacement);
    }

   private PipeWrp GetOriginalPipe(Element selectedElement, XYZ pick, out DisplacementElement primaryDisplacement)
{
    Document doc = selectedElement.Document;
    PipeWrp originalPipe = null;
    primaryDisplacement = null;

    switch (selectedElement)
    {
        case Pipe pipe:
            originalPipe = new PipeWrp(pipe);
            break;

        case DisplacementElement displacementElement:
        {
            primaryDisplacement = displacementElement;
            var displacementElementIds = displacementElement.GetDisplacedElementIds();

            foreach (ElementId displacedId in displacementElementIds)
            {
                Element element = doc.GetElement(displacedId);
                if (element is not Pipe pipe) continue;

                // Попробуем несколько способов проверки
                if (IsPipeAtPoint(pipe, pick, doc))
                {
                    originalPipe = new PipeWrp(pipe);
                    break;
                }
            }
            break;
        }
    }

    return originalPipe;
}

private bool IsPipeAtPoint(Pipe pipe, XYZ pick, Document doc)
{
    // Способ 1: Проверка через BoundingBox с толерантностью
    BoundingBoxXYZ bounding = pipe.get_BoundingBox(doc.ActiveView);
    if (bounding != null)
    {
        // Расширяем bounding box на небольшое значение
        double tolerance = 0.1; // футы, настройте под ваши нужды
        XYZ min = bounding.Min - new XYZ(tolerance, tolerance, tolerance);
        XYZ max = bounding.Max + new XYZ(tolerance, tolerance, tolerance);

        if (pick.X >= min.X && pick.X <= max.X &&
            pick.Y >= min.Y && pick.Y <= max.Y &&
            pick.Z >= min.Z && pick.Z <= max.Z)
        {
            return true;
        }
    }

    // Способ 2: Проверка через геометрию (более точный)
    try
    {
        Options geometryOptions = new Options
        {
            DetailLevel = ViewDetailLevel.Medium,
            IncludeNonVisibleObjects = false
        };

        GeometryElement geometryElement = pipe.get_Geometry(geometryOptions);
        if (geometryElement != null)
        {
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (IsPointNearGeometry(geometryObject, pick))
                {
                    return true;
                }
            }
        }
    }
    catch (Exception ex)
    {
        // Логирование ошибки
        System.Diagnostics.Debug.WriteLine($"Error checking pipe geometry: {ex.Message}");
    }

    // Способ 3: Проверка через LocationCurve (для труб)
    try
    {
        if (pipe.Location is LocationCurve locationCurve)
        {
            Curve curve = locationCurve.Curve;
            double distance = curve.Distance(pick);
            double pipeRadius = pipe.Diameter / 2.0;

            // Проверяем, находится ли точка в пределах радиуса трубы + толерантность
            if (distance <= pipeRadius + 0.1) // 0.1 фута толерантность
            {
                return true;
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error checking pipe location: {ex.Message}");
    }

    return false;
}

private bool IsPointNearGeometry(GeometryObject geometryObject, XYZ point)
{
    double tolerance = 0.1; // футы

    switch (geometryObject)
    {
        case Solid solid:
            // Проверяем, находится ли точка внутри или рядом с solid
            try
            {
                BoundingBoxXYZ bbox = solid.GetBoundingBox();
                if (bbox != null)
                {
                    XYZ min = bbox.Min - new XYZ(tolerance, tolerance, tolerance);
                    XYZ max = bbox.Max + new XYZ(tolerance, tolerance, tolerance);

                    return point.X >= min.X && point.X <= max.X &&
                           point.Y >= min.Y && point.Y <= max.Y &&
                           point.Z >= min.Z && point.Z <= max.Z;
                }
            }
            catch { }
            break;

        case Curve curve:
            return curve.Distance(point) <= tolerance;

        case GeometryInstance instance:
            Transform transform = instance.Transform;
            foreach (GeometryObject obj in instance.GetInstanceGeometry())
            {
                XYZ transformedPoint = transform.Inverse.OfPoint(point);
                if (IsPointNearGeometry(obj, transformedPoint))
                    return true;
            }
            break;
    }

    return false;
}
}