using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoNameAPI.Utils;

namespace CopyElementsByLevel.Models
{
    public class MepCurveWr
    {
        public MepCurveWr(MEPCurve mepCurve)
        {
            MepCurve = mepCurve;
            Id = mepCurve.Id;
            FirstConnector = mepCurve.GetNearestConnector(StartPoint);
            SecondConnector = mepCurve.GetNearestConnector(EndPoint);
        }

        public ElementId Id { get; }

        public Connector FirstConnector { get; }

        public Connector SecondConnector { get; }

        private Curve Curve => ((LocationCurve)MepCurve.Location).Curve;

        private XYZ StartPoint => Curve.GetEndPoint(0);

        private XYZ EndPoint => Curve.GetEndPoint(1);

        private MEPCurve MepCurve { get; }

        public XYZ GetNearestEndPoint(XYZ point) => StartPoint.DistanceTo(point) >= EndPoint.DistanceTo(point) ? EndPoint : StartPoint;

        public Connector GetConnectorByDirection(XYZ direction) => FirstConnector.CoordinateSystem.BasisZ.IsAlmostEqualTo(direction, 0.001) ? FirstConnector : SecondConnector;

        public void StretchToPoint(XYZ pointCurve, XYZ newPoint)
        {
            if (!(Curve is Line))
                throw new ArgumentOutOfRangeException("LocationCurve", "Location curve of MEPCurve is not a line");
            XYZ source = SecondConnector.Origin.DistanceTo(pointCurve) >= FirstConnector.Origin.DistanceTo(pointCurve) ? FirstConnector.Origin : SecondConnector.Origin;
            SetCurve(Line.CreateBound(StartPoint.DistanceTo(source) > EndPoint.DistanceTo(source) ? newPoint : StartPoint, StartPoint.DistanceTo(source) > EndPoint.DistanceTo(source) ? EndPoint : newPoint));
        }

        public bool IsPointOnCurve(XYZ point)
        {
            if (point.DistanceTo(StartPoint) <= 0.0001 || point.DistanceTo(EndPoint) <= 0.0001)
                return false;
            IntersectionResult intersectionResult = Curve.Project(point);
            return intersectionResult != null && intersectionResult.Distance < 0.0001;
        }

        private void SetCurve(Curve curve) => ((LocationCurve)MepCurve.Location).Curve = curve;
    }
}
