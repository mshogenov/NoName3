namespace MepElementsCopy.Models;

public class MepCurveMdl
{
   public ElementId Id { get; }

    public Connector FirstConnector { get; }

    public Connector SecondConnector { get; }

    private Curve Curve
    {
        get
        {
            if (MepCurve?.Location is not LocationCurve)
                return null;

            var locationCurve = MepCurve.Location as LocationCurve;
            try
            {
                return locationCurve?.Curve;
            }
            catch (Autodesk.Revit.Exceptions.InvalidObjectException)
            {
                return null;
            }
        }
    }

    private XYZ StartPoint => Curve.GetEndPoint(0);

    private XYZ EndPoint => Curve.GetEndPoint(1);

    public MEPCurve MepCurve { get; }

    public MepCurveMdl(MEPCurve mepCurve)
    {
        MepCurve = mepCurve;
        Id = mepCurve.Id;
        FirstConnector = GetNearestConnector(mepCurve,StartPoint);
        SecondConnector = GetNearestConnector(mepCurve,EndPoint);
    }
    public XYZ GetNearestEndPoint(XYZ point)
    {
        return StartPoint.DistanceTo(point) >= EndPoint.DistanceTo(point) ? EndPoint : StartPoint;
    }

    public Connector GetConnectorByDirection(XYZ direction)
    {
        return FirstConnector.CoordinateSystem.BasisZ.IsAlmostEqualTo(direction, 0.001)
            ? FirstConnector
            : SecondConnector;
    }

    public void StretchToPoint(XYZ pointCurve, XYZ newPoint)
    {
        if (Curve is not Line)
            throw new ArgumentOutOfRangeException("LocationCurve", "Location curve of MEPCurve is not a line");
        XYZ source = SecondConnector.Origin.DistanceTo(pointCurve) >= FirstConnector.Origin.DistanceTo(pointCurve) ? FirstConnector.Origin : SecondConnector.Origin;
        SetCurve(Line.CreateBound(StartPoint.DistanceTo(source) > EndPoint.DistanceTo(source) ? newPoint : StartPoint, StartPoint.DistanceTo(source) > EndPoint.DistanceTo(source) ? EndPoint : newPoint));
    }

    public bool IsPointOnCurve( XYZ point)
    {
        // Используем системную точность
        const double tolerance = 0.0001;

        // Исключаем начальную и конечную точки кривой
        if (point.DistanceTo(StartPoint) <= tolerance || point.DistanceTo(EndPoint) <= tolerance)
            return false;

        // Проецируем точку на кривую
        IntersectionResult intersectionResult = Curve.Project(point);
        if (intersectionResult == null)
            return false;

        // Проверяем расстояние от точки до кривой
        if (intersectionResult.Distance > tolerance)
            return false;

        // Проверяем, что параметр находится внутри кривой (исключая концы)
        double param0 = Curve.GetEndParameter(0);
        double param1 = Curve.GetEndParameter(1);
        double projectedParam = intersectionResult.Parameter;

        if (projectedParam <= param0 || projectedParam >= param1)
            return false;

        // Точка находится на кривой
        return true;
    }

    private void SetCurve(Curve curve)
    {
        ((LocationCurve)MepCurve.Location).Curve = curve;
    }

    private static Connector GetNearestConnector( MEPCurve mepCurve, XYZ point)
    {
        Connector nearestConnector = null;
        double d = double.NaN;
        foreach (Connector connector in mepCurve.ConnectorManager.Connectors.OfType<Connector>())
        {
            double num = point.DistanceTo(connector.Origin);
            if (!double.IsNaN(d) && !(num < d)) continue;
            d = num;
            nearestConnector = connector;
        }
        return nearestConnector;
    }
   
}