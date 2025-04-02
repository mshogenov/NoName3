using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyElementsByLevel.Models
{
    public class ConnectorSplitWr
    {
        public ConnectorSplitWr(Connector connector, ElementId idMepCurve)
        {
            Connector = connector;
            IdMepCurve = idMepCurve;
            OwnerId = connector.Owner.Id;
            Direction = connector.CoordinateSystem.BasisZ;
            Origin = connector.Origin;
        }

        public ElementId IdMepCurve { get; }

        public XYZ Direction { get; }

        public XYZ Origin { get; }

        public Connector Connector { get; }

        public ElementId OwnerId { get; }

        public bool AreOpposite(Connector connector) => Math.Abs(Direction.DotProduct(connector.CoordinateSystem.BasisZ) + 1.0) < 0.001;

        public double GetDistance(ConnectorSplitWr connector) => Origin.DistanceTo(connector.Origin);
    }
}
