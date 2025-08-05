using Autodesk.Revit.DB.Plumbing;
using NoNameApi.Extensions;

namespace MakeBreak.Models;

public class Gap
{
    public FamilyInstance FamilyInstance { get; set; }
    public List<Element> ConnectedElements => GetConnectedElements();
    public ElementId Id { get; set; }
    public List<Connector> Connectors => [..GetConnectors()];

    public Gap(Reference reference, Document doc)
    {
        if (reference == null)
        {
            return;
        }

        Element element = doc.GetElement(reference);
        switch (element)
        {
            case FamilyInstance familyInstance:
                if (familyInstance.Name == "Разрыв")
                {
                    FamilyInstance = familyInstance;
                    Id = familyInstance.Id;
                }

                break;
            case DisplacementElement displacement:
                var pickPoint = reference.GlobalPoint;
                var family = FindElementInDisplacement(displacement, pickPoint, doc);
                if (family == null) break;
                if (family.Name == "Разрыв")
                {
                    FamilyInstance = family;
                    Id = family.Id;
                }

                break;
            default: return;
        }
    }

    private FamilyInstance FindElementInDisplacement(DisplacementElement displacement, XYZ pickPoint, Document doc)
    {
        var displacementElementIds = displacement.GetDisplacedElementIds();
        double toleranceInMm = 200;
        double tolerance = UnitUtils.ConvertToInternalUnits(toleranceInMm, UnitTypeId.Millimeters);

        foreach (ElementId displacedId in displacementElementIds)
        {
            Element element = doc.GetElement(displacedId);
            if (element is not FamilyInstance instance) continue;

            if (IsFamilyInstanceAtPoint(instance, pickPoint, tolerance))
            {
                return instance;
            }
        }

        return null;
    }

    private bool IsFamilyInstanceAtPoint(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        // Способ 1: Расширенный BoundingBox
        if (CheckBoundingBoxContains(instance, pickPoint, tolerance))
            return true;

        // Способ 2: Проверка через геометрию
        if (CheckGeometryContains(instance, pickPoint, tolerance))
            return true;

        // Способ 3: Проверка через Location (для точечных элементов)
        if (CheckLocationDistance(instance, pickPoint, tolerance))
            return true;

        return false;
    }

    private bool CheckBoundingBoxContains(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        try
        {
            BoundingBoxXYZ bounding = instance.get_BoundingBox(instance.Document.ActiveView);

            // Если bounding для активного вида null, попробуем без вида
            if (bounding == null)
            {
                bounding = instance.get_BoundingBox(null);
            }

            if (bounding == null) return false;

            // Расширяем BoundingBox
            XYZ expandVector = new XYZ(tolerance, tolerance, tolerance);

            XYZ min = bounding.Min.Subtract(expandVector);
            XYZ max = bounding.Max.Add(expandVector);

            return pickPoint.X >= min.X && pickPoint.X <= max.X &&
                   pickPoint.Y >= min.Y && pickPoint.Y <= max.Y &&
                   pickPoint.Z >= min.Z && pickPoint.Z <= max.Z;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckBoundingBoxContains: {ex.Message}");
            return false;
        }
    }

    private bool CheckGeometryContains(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        try
        {
            Options geometryOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Medium,
                IncludeNonVisibleObjects = false,
                ComputeReferences = false
            };

            GeometryElement geometryElement = instance.get_Geometry(geometryOptions);
            if (geometryElement == null) return false;

            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (IsPointNearFamilyGeometry(geometryObject, pickPoint, tolerance))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckGeometryContains: {ex.Message}");
        }

        return false;
    }

    private bool CheckLocationDistance(FamilyInstance instance, XYZ pickPoint, double tolerance)
    {
        try
        {
            if (instance.Location is LocationPoint locationPoint)
            {
                double distance = locationPoint.Point.DistanceTo(pickPoint);
                return distance <= tolerance;
            }
            else if (instance.Location is LocationCurve locationCurve)
            {
                Curve curve = locationCurve.Curve;
                double distance = curve.Distance(pickPoint);
                return distance <= tolerance;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckLocationDistance: {ex.Message}");
        }

        return false;
    }

    private bool IsPointNearFamilyGeometry(GeometryObject geometryObject, XYZ point, double tolerance)
    {
        try
        {
            switch (geometryObject)
            {
                case Solid solid when solid.Volume > 0:
                    return IsPointNearSolid(solid, point, tolerance);

                case GeometryInstance instance:
                    Transform transform = instance.Transform;
                    foreach (GeometryObject obj in instance.GetInstanceGeometry())
                    {
                        // Преобразуем точку в локальную систему координат
                        XYZ localPoint = transform.Inverse.OfPoint(point);
                        if (IsPointNearFamilyGeometry(obj, localPoint, tolerance))
                            return true;
                    }

                    break;

                case Curve curve:
                    return curve.Distance(point) <= tolerance;

                case Face face:
                    try
                    {
                        IntersectionResult result = face.Project(point);
                        if (result != null)
                        {
                            return result.Distance <= tolerance;
                        }
                    }
                    catch
                    {
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in IsPointNearFamilyGeometry: {ex.Message}");
        }

        return false;
    }

    private bool IsPointNearSolid(Solid solid, XYZ point, double tolerance)
    {
        try
        {
            // Сначала проверяем расширенный BoundingBox
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            if (bbox != null)
            {
                XYZ min = bbox.Min - new XYZ(tolerance, tolerance, tolerance);
                XYZ max = bbox.Max + new XYZ(tolerance, tolerance, tolerance);

                bool inExpandedBox = point.X >= min.X && point.X <= max.X &&
                                     point.Y >= min.Y && point.Y <= max.Y &&
                                     point.Z >= min.Z && point.Z <= max.Z;

                if (!inExpandedBox) return false;
            }

            // Проверяем каждую грань solid'а
            foreach (Face face in solid.Faces)
            {
                try
                {
                    IntersectionResult result = face.Project(point);
                    if (result != null && result.Distance <= tolerance)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in IsPointNearSolid: {ex.Message}");
        }

        return false;
    }

    private Gap(FamilyInstance familyInstance)
    {
        if (familyInstance == null) return;
        FamilyInstance = familyInstance;
        Id = familyInstance.Id;
    }

    private List<Element> GetConnectedElements()
    {
        var elements = new List<Element>();
        try
        {
            foreach (var connector in Connectors)
            {
                if (!connector.IsConnected) continue;
                foreach (Connector connectedConnector in connector.AllRefs.Cast<Connector>())
                {
                    if (connectedConnector.Owner.Id != this.Id)
                    {
                        elements.Add(connectedConnector.Owner);
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }

        return elements;
    }

    private IEnumerable<Connector> GetConnectors()
    {
        if (FamilyInstance?.MEPModel?.ConnectorManager?.Connectors == null)
            yield break;

        foreach (Connector connector in FamilyInstance.MEPModel.ConnectorManager.Connectors)
        {
            yield return connector;
        }
    }

    public Gap FindPairBreak(FamilySymbol familySymbol)
    {
        var breakLists = new List<List<FamilyInstance>>();
        var visitedElements = new HashSet<ElementId>(); // Для отслеживания просмотренных элементов

        foreach (var element in ConnectedElements)
        {
            var breaksInPath = new List<FamilyInstance>();
            visitedElements.Clear(); // Очищаем для каждого нового пути
            visitedElements.Add(Id); // Добавляем исходный разрыв
            FindBreaksInPath(element, familySymbol, breaksInPath, 0, visitedElements);

            if (breaksInPath.Count != 0)
            {
                breakLists.Add(breaksInPath);
            }
        }

        switch (breakLists.Count)
        {
            // Остальная логика выбора парного разрыва...
            case 0:
                return null;
            case 1: return new Gap(breakLists[0][0]);
            default:
            {
                var oddList = breakLists.FirstOrDefault(list => list.Count % 2 != 0);
                return oddList != null ? new Gap(oddList[0]) : null;
            }
        }
    }

    private void FindBreaksInPath(Element element, FamilySymbol familySymbol,
        List<FamilyInstance> breaks, int depth, HashSet<ElementId> visitedElements)
    {
        if (element == null || !visitedElements.Add(element.Id)) return;

        if (depth > 2 && breaks.Count == 0) return;

        if (element is FamilyInstance familyInstance && familyInstance.Name == familySymbol.Name)
        {
            breaks.Add(familyInstance);
            depth = 0;
        }

        var elementsConnected = element.GetConnectedMEPElements()
            .Where(connectedElement => !visitedElements.Contains(connectedElement.Id));
        foreach (var connectedElement in elementsConnected)
        {
            FindBreaksInPath(connectedElement, familySymbol, breaks, depth + 1, visitedElements);
        }
    }

    public Pipe FindGeneralPipe(Gap gap)
    {
        foreach (var connectedElement in ConnectedElements)
        {
            if (connectedElement is not Pipe pipe) continue;
            if (gap.ConnectedElements.FirstOrDefault(x => x.Id == pipe.Id) is Pipe generalPipe)
            {
                return generalPipe;
            }
        }

        return null;
    }
}