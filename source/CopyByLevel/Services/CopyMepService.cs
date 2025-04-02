using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CopyByLevel.Models;

namespace CopyByLevel.Services
{
    public class CopyMepService : BaseService
    {

        private List<ElemWr> _mepElWrappers;
        private readonly List<MepCurveWr> _mepCurves;
        private XYZ _direction;
        private readonly Options _options;

        public CopyMepService()
        {
            _mepCurves = Collector.OfClass(typeof(MEPCurve)).OfType<MEPCurve>().Where(m =>
            {
                switch (m)
                {
                    case DuctInsulation _:
                    case PipeInsulation _:
                    case FlexDuct _:
                    case FlexPipe _:
                        return false;
                    default:
                        return !(m is InsulationLiningBase);
                }
            }).Select(m => new MepCurveWr(m)).ToList();
            _options = new Options()
            {
                IncludeNonVisibleObjects = false,
                DetailLevel = (ViewDetailLevel)3
            };
        }

        public List<LevelWr> LevelWrs { get; private set; }

        public bool FillMepElements()
        {
            _mepElWrappers = new List<ElemWr>();
            foreach (ElementId elementId in (IEnumerable<ElementId>)UiDocument.Selection.GetElementIds())
            {
                Element element = Document.GetElement(elementId);
                if (MepElemSelectionFilter.AllowElement(element))
                    _mepElWrappers.Add(new ElemWr(element));
            }
            if (_mepElWrappers.Count == 0)
                _mepElWrappers = UiDocument.Selection.PickObjects((ObjectType)1, MepElemSelectionFilter).Select(p => new ElemWr(Document.GetElement(p))).ToList();
            return _mepElWrappers.Count > 0;
        }

        //public void SelectDirection()
        //{
        //    Reference reference = UiDocument.Selection.PickObject((ObjectType)1, (ISelectionFilter)new MepCurveSelectionFilter());
        //    Curve curve = ((LocationCurve)Document.GetElement(reference).Location).Curve;
        //    curve.MakeUnbound();
        //    XYZ xyzPoint1 = curve.Project(reference.GlobalPoint).XYZPoint;
        //    //XYZ furthestPoint = FindFurthestPoint(_mepElWrappers, xyzPoint1);
        //    XYZ xyzPoint2 = curve.Project(furthestPoint).XYZPoint;
        //    _direction = (xyzPoint1- xyzPoint2).Normalize();
        //}

        public void CopyMepElementsToLevels(IEnumerable<LevelWr> selectedLevelItems)
        {
            ElemWr elemWr = _mepElWrappers.Where(m => m.BindingLevel != null).OrderBy(m => m.BindingLevel.Elevation).FirstOrDefault();
            if (elemWr == null)
            {
                MessageBox.Show("Ошибка", "");
            }
            else
            {
                Level bindingLevel = elemWr.BindingLevel;
                ICollection<ElementId> elementIds = null;
                using (Transaction transaction = new Transaction(Document, "Копирование по уровню"))
                {
                    transaction.Start();
                    foreach (LevelWr selectedLevelItem in selectedLevelItems)
                        elementIds = CopyingMepElementsAndConnect(selectedLevelItem.Elevation - bindingLevel.Elevation);
                    transaction.Commit();
                }
                if (elementIds == null)
                    return;
                UiDocument.Selection.SetElementIds(elementIds);
            }
        }

        public void CopyMepElementsToDistance(CopyByDistanceUserConfig userConfig)
        {
            ICollection<ElementId> elementIds = null;
            double ft = userConfig.Distance.ToMillimeters();
            using (Transaction transaction = new Transaction(Document, "Копировать по вертикали"))
            {
                transaction.Start();
                for (int countBelow = userConfig.CountBelow; countBelow >= 1; --countBelow)
                    elementIds = CopyingMepElementsAndConnect(-ft * countBelow);
                for (int index = 1; index <= userConfig.CountAbove; ++index)
                    elementIds = CopyingMepElementsAndConnect(ft * index);
                transaction.Commit();
            }
            if (elementIds == null)
                return;
            UiDocument.Selection.SetElementIds(elementIds);
        }

        public void CopyMepElementsByDirection(CopyByDirectionUserConfig userConfig)
        {
            ICollection<ElementId> elementIds = null;
            double ft = userConfig.Distance.ToMillimeters();
            using (Transaction transaction = new Transaction(Document,"Копировать по направлению"))
            {
                transaction.Start();
                for (int index = 1; index <= userConfig.Count; ++index)
                    elementIds = CopyingMepElementsAndConnect(ft * index);
                transaction.Commit();
            }
            if (elementIds == null)
                return;
            UiDocument.Selection.SetElementIds(elementIds);
        }
        public void FillLevelWrs() => LevelWrs = Collector.OfClass(typeof(Level)).OfType<Level>().OrderBy(level => level.Elevation).Select(level => new LevelWr(level)).ToList();
        //private XYZ FindFurthestPoint(List<ElemWr> elements, XYZ fromPoint)
        //{
        //    XYZ furthestPoint = null;
        //    double minValue = double.MinValue;
        //    foreach (ElemWr element in elements)
        //    {
        //        foreach (GeometryObject geomObj in ((IEnumerable<GeometryObject>)element.Element[this._options]).ToList<GeometryObject>())


        //            this.ProcessGeometryObject(geomObj, fromPoint, ref furthestPoint, ref minValue);

        //    }
        //    return furthestPoint;
        //}
        private void ProcessGeometryObject(
        GeometryObject geomObj,
        XYZ fromPoint,
          ref XYZ furthestPoint,
          ref double maxDistance)
        {
            switch (geomObj)
            {
                case Solid solid:
                    IEnumerator enumerator1 = solid.Edges.GetEnumerator();
                    try
                    {
                        while (enumerator1.MoveNext())
                        {
                            Edge current = (Edge)enumerator1.Current;
                            EvaluatePoint(current.AsCurve().GetEndPoint(0), fromPoint, ref furthestPoint, ref maxDistance);
                            EvaluatePoint(current.AsCurve().GetEndPoint(1), fromPoint, ref furthestPoint, ref maxDistance);
                        }
                        break;
                    }
                    finally
                    {
                        if (enumerator1 is IDisposable disposable)
                            disposable.Dispose();
                    }
                case Curve curve:
                    if (!curve.IsBound)
                        break;
                    EvaluatePoint(curve.GetEndPoint(0), fromPoint, ref furthestPoint, ref maxDistance);
                    EvaluatePoint(curve.GetEndPoint(1), fromPoint, ref furthestPoint, ref maxDistance);
                    break;
                case GeometryInstance geometryInstance:
                    using (IEnumerator<GeometryObject> enumerator2 = geometryInstance.GetInstanceGeometry().GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                            ProcessGeometryObject(enumerator2.Current, fromPoint, ref furthestPoint, ref maxDistance);
                        break;
                    }
            }
        }

        private void EvaluatePoint(
          XYZ point,
          XYZ fromPoint,
          ref XYZ furthestPoint,
          ref double maxDistance)
        {
            double num = point.DistanceTo(fromPoint);
            if (num <= maxDistance)
                return;
            maxDistance = num;
            furthestPoint = point;
        }

        private ICollection<ElementId> CopyingMepElementsAndConnect(double offset)
        {
            XYZ xyz = (_direction ?? XYZ.BasisZ).Multiply(offset);
            ICollection<ElementId> mepElementsIds = ElementTransformUtils.CopyElements(Document, _mepElWrappers.Select(m => m.Id).ToList(), xyz);
            List<Tuple<ConnectorSplitWr, ConnectorSplitWr>> splitConnectors = GetSplitConnectors(mepElementsIds);
            if (splitConnectors != null)
                ConnectInMepCurves(splitConnectors);
            return mepElementsIds;
        }

        private void ConnectInMepCurves(
          List<Tuple<ConnectorSplitWr, ConnectorSplitWr>> splitConnectorsPairs)
        {
            foreach (Tuple<ConnectorSplitWr, ConnectorSplitWr> splitConnectorsPair in splitConnectorsPairs)
            {
                ConnectorSplitWr connectorSplitWr1;
                ConnectorSplitWr connectorSplitWr2;
                splitConnectorsPair.Deconstruct(out connectorSplitWr1, out connectorSplitWr2);
                ConnectorSplitWr first = connectorSplitWr1;
                ConnectorSplitWr connectorSplitWr3 = connectorSplitWr2;
                MepCurveWr mWr = _mepCurves.FirstOrDefault(m => (m.Id == first.IdMepCurve));
                if (mWr != null)
                {
                    Connector opositeConnector = mWr.GetConnectorByDirection(connectorSplitWr3.Direction);
                    Connector connectorByDirection = mWr.GetConnectorByDirection(first.Direction);
                    XYZ nearestEndPoint1 = mWr.GetNearestEndPoint(opositeConnector.Origin);
                    XYZ nearestEndPoint2 = mWr.GetNearestEndPoint(connectorByDirection.Origin);
                    Connector connector = opositeConnector.AllRefs.OfType<Connector>().FirstOrDefault(c => (c.Owner.Id != opositeConnector.Owner.Id) && c.ConnectorType == ConnectorType.End);
                    mWr.StretchToPoint(nearestEndPoint2, first.Connector.Origin);
                    first.Connector.ConnectTo(opositeConnector);
                    MEPCurve curve = this.CreateCurve(connectorSplitWr3.Connector.Origin, nearestEndPoint1, mWr);
                    if (curve != null)
                    {
                        MepCurveWr mepCurveWr = new MepCurveWr(curve);
                        _mepCurves.Add(mepCurveWr);
                        if (connector != null)
                            mepCurveWr.GetConnectorByDirection(connectorSplitWr3.Direction).ConnectTo(connector);
                        connectorSplitWr3.Connector.ConnectTo(mepCurveWr.GetConnectorByDirection(first.Direction));
                        
                    }
                }
            }
        }

        private List<Tuple<ConnectorSplitWr, ConnectorSplitWr>> GetSplitConnectors(
          IEnumerable<ElementId> mepElementsIds)
        {
            List<ConnectorSplitWr> connectorSplitWrs = new List<ConnectorSplitWr>();
            foreach (ElementId mepElementsId in mepElementsIds)
            {
                Element element = Document.GetElement(mepElementsId);
                if (element is FamilyInstance familyInstance)
                {
                    if (familyInstance.MEPModel != null)
                    {
                        ConnectorManager connectorManager = familyInstance.MEPModel.ConnectorManager;
                        if (connectorManager != null)
                        {
                            foreach (Connector connector in connectorManager.Connectors.OfType<Connector>().Where(c => c.ConnectorType == ConnectorType.End))
                            {
                                ElementId idMepCurve;
                                if (!connector.IsConnected && IsInMepCurve(connector, out idMepCurve))
                                    connectorSplitWrs.Add(new ConnectorSplitWr(connector, idMepCurve));
                            }
                        }
                    }
                }
                else if (element is MEPCurve mepCurve)
                {
                    foreach (Connector connector in mepCurve.ConnectorManager.Connectors.OfType<Connector>().Where(c => c.ConnectorType == ConnectorType.End))
                    {
                        ElementId idMepCurve;
                        if (!connector.IsConnected && IsInMepCurve(connector, out idMepCurve))
                            connectorSplitWrs.Add(new ConnectorSplitWr(connector, idMepCurve));
                    }
                    _mepCurves.Add(new MepCurveWr(mepCurve));
                }
            }
            return connectorSplitWrs.Count < 2 ? null : ConnectorPairFinder(connectorSplitWrs);
        }

        private List<Tuple<ConnectorSplitWr, ConnectorSplitWr>> ConnectorPairFinder(
          List<ConnectorSplitWr> connectorSplitWrs)
        {
            List<IGrouping<ElementId, ConnectorSplitWr>> list1 = connectorSplitWrs.GroupBy(c => c.IdMepCurve).Where(g => g.Count() > 1).ToList();
            if (!list1.Any())
                return null;
            List<Tuple<ConnectorSplitWr, ConnectorSplitWr>> tupleList = new List<Tuple<ConnectorSplitWr, ConnectorSplitWr>>();
            foreach (IGrouping<ElementId, ConnectorSplitWr> source in list1)
            {
                double num = double.MaxValue;
                List<ConnectorSplitWr> list2 = source.ToList();
                Tuple<ConnectorSplitWr, ConnectorSplitWr> tuple = null;
                for (int index1 = 0; index1 < list2.Count; ++index1)
                {
                    for (int index2 = index1 + 1; index2 < list2.Count; ++index2)
                    {
                        ConnectorSplitWr connectorSplitWr = list2[index1];
                        ConnectorSplitWr connector = list2[index2];
                        if (connectorSplitWr.AreOpposite(connector.Connector))
                        {
                            double distance = connectorSplitWr.GetDistance(connector);
                            if (distance < num)
                            {
                                num = distance;
                                tuple = new Tuple<ConnectorSplitWr, ConnectorSplitWr>(connectorSplitWr, connector);
                            }
                        }
                    }
                }
                if (tuple != null)
                    tupleList.Add(tuple);
            }
            return tupleList;
        }

        private bool IsInMepCurve(Connector c, out ElementId idMepCurve)
        {
            idMepCurve = _mepCurves.FirstOrDefault(m => m.IsPointOnCurve(c.Origin) && SameConnector(c, m.FirstConnector))?.Id;
            return (idMepCurve!= null);
        }

        private bool SameConnector(Connector connector1, Connector connector2) => connector1.Domain == connector2.Domain && connector1.Shape == connector2.Shape && (connector1.Shape != null || Math.Abs(connector1.Radius - connector2.Radius) <= 0.01) && (connector1.Shape - 1 > ConnectorProfileType.Rectangular || Math.Abs(connector1.Width - connector2.Width) <= 0.01 && Math.Abs(connector1.Height - connector2.Height) <= 0.01);

        private MEPCurve CreateCurve(XYZ p1, XYZ p2, MepCurveWr mWr)
        {
            if (p1.DistanceTo(p2) < 0.1)
                return null;
            ElementId elementId = ElementTransformUtils.CopyElement(Document, mWr.Id, XYZ.Zero).FirstOrDefault();
            MEPCurve element=null;
            int num;
            if (!(elementId== null))
            {
                element = Document.GetElement(elementId) as MEPCurve;
                num = element == null ? 1 : 0;
            }
            else
                num = 1;
            if (num != 0)
                return null;
            Curve curve = ((LocationCurve)element.Location).Curve;
            XYZ xyz1 = p2.DistanceTo(curve.GetEndPoint(0)) > p2.DistanceTo(curve.GetEndPoint(1)) ? p1 : p2;
            XYZ xyz2 = xyz1.IsAlmostEqualTo(p1) ? p2 : p1;
            ((LocationCurve)element.Location).Curve = Line.CreateBound(xyz1, xyz2);
            return element;
        }
    }
}
