using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
    public class MepCurveWr
    {
        public MepCurveWr(MEPCurve mepCurve)
        {
            MepCurve = mepCurve;
            Id = mepCurve.Id;

            // Вычисляем начальную и конечную точки до вызова метода GetNearestConnector
            var startPoint = this.Curve.GetEndPoint(0);
            var endPoint = this.Curve.GetEndPoint(1);

           FirstConnector = GetNearestConnector(mepCurve, startPoint);
           SecondConnector = GetNearestConnector(mepCurve, endPoint);
        }

        public ElementId Id { get; }

        public Connector FirstConnector { get; }

        public Connector SecondConnector { get; }

        private Curve Curve => ((LocationCurve)MepCurve.Location).Curve;

        private XYZ StartPoint => this.Curve.GetEndPoint(0);

        private XYZ EndPoint => this.Curve.GetEndPoint(1);

        private MEPCurve MepCurve { get; }

        public XYZ GetNearestEndPoint(XYZ point) => this.StartPoint.DistanceTo(point) >= this.EndPoint.DistanceTo(point) ? this.EndPoint : this.StartPoint;

        public Connector GetConnectorByDirection(XYZ direction) => this.FirstConnector.CoordinateSystem.BasisZ.IsAlmostEqualTo(direction, 0.001) ? this.FirstConnector : this.SecondConnector;

        public void StretchToPoint(XYZ pointCurve, XYZ newPoint)
        {
            if (!(this.Curve is Line))
                throw new ArgumentOutOfRangeException("LocationCurve", "Location curve of MEPCurve is not a line");
            XYZ xyz = this.SecondConnector.Origin.DistanceTo(pointCurve) >= this.FirstConnector.Origin.DistanceTo(pointCurve) ? this.FirstConnector.Origin : this.SecondConnector.Origin;
            this.SetCurve(Line.CreateBound(this.StartPoint.DistanceTo(xyz) > this.EndPoint.DistanceTo(xyz) ? newPoint : this.StartPoint, this.StartPoint.DistanceTo(xyz) > this.EndPoint.DistanceTo(xyz) ? this.EndPoint : newPoint));
        }

        public bool IsPointOnCurve(XYZ point)
        {
            if (point.DistanceTo(this.StartPoint) <= 0.0001 || point.DistanceTo(this.EndPoint) <= 0.0001)
                return false;
            IntersectionResult intersectionResult = this.Curve.Project(point);
            return intersectionResult != null && intersectionResult.Distance < 0.0001;
        }

        private void SetCurve(Curve curve) => ((LocationCurve)MepCurve.Location).Curve = curve;
        private Connector GetNearestConnector(MEPCurve mepCurve, XYZ point)
        {
            ConnectorSet connectors = mepCurve.ConnectorManager.Connectors;
            Connector nearestConnector = null;
            double minDistance = double.MaxValue;

            foreach (Connector connector in connectors)
            {
                double distance = connector.Origin.DistanceTo(point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestConnector = connector;
                }
            }

            return nearestConnector;
        }
    }

}