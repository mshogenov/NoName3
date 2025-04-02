using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Models
{
    public class ConnectorSplitWr
    {
        public ConnectorSplitWr(Connector connector, ElementId idMepCurve)
        {
            this.Connector = connector;
            this.IdMepCurve = idMepCurve;
            this.OwnerId = connector.Owner.Id;
            this.Direction = connector.CoordinateSystem.BasisZ;
            this.Origin = connector.Origin;
        }

        public ElementId IdMepCurve { get; }

        public XYZ Direction { get; }

        public XYZ Origin { get; }

        public Connector Connector { get; }

        public ElementId OwnerId { get; }

        public bool AreOpposite(Connector connector) => Math.Abs(this.Direction.DotProduct(connector.CoordinateSystem.BasisZ) + 1.0) < 0.001;

        public double GetDistance(ConnectorSplitWr connector) => this.Origin.DistanceTo(connector.Origin);
    }
}
