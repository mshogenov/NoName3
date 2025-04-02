using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CopyElementsByLevel.Models;
using CopyElementsByLevel.SelectionFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CopyElementsByLevel.Services
{
    public class CopyMepService 
    {
        private Document doc=Context.ActiveDocument;
        private UIDocument uidoc = Context.ActiveUiDocument;
        private List<ElemWr> _mepElWrappers;
        private readonly List<MepCurveWr> _mepCurves;
        public List<LevelWr> LevelWrs { get; private set; }
        private XYZ _direction;

        public void FillLevelWrs() => LevelWrs = new FilteredElementCollector(doc).OfClass(typeof(Level)).OfType<Level>().OrderBy(level => level.Elevation).Select(level => new LevelWr(level)).ToList();

        public bool FillMepElements()
        {
            _mepElWrappers = [];
            foreach (ElementId elementId in (IEnumerable<ElementId>)uidoc.Selection.GetElementIds())
            {
                Element element = doc.GetElement(elementId);
                if (new MepElemSelectionFilter().AllowElement(element))
                    _mepElWrappers.Add(new ElemWr(element));
            }
            if (_mepElWrappers.Count == 0)
                _mepElWrappers = uidoc.Selection.PickObjects(ObjectType.Element, new MepElemSelectionFilter()).Select(p => new ElemWr(doc.GetElement(p))).ToList();
            return _mepElWrappers.Count > 0;
        }
        public void CopyMepElementsToLevels(IEnumerable<LevelWr> selectedLevelItems)
        {
            ElemWr elemWr = _mepElWrappers.Where(m => m.BindingLevel != null).OrderBy(m => m.BindingLevel.Elevation).FirstOrDefault();
            if (elemWr == null)
            {
               
            }
            else
            {
                Level bindingLevel = elemWr.BindingLevel;
                ICollection<ElementId> elementIds = null;
                using (Transaction transaction = new Transaction(doc, " "))
                {
                    int num1 = (int)transaction.Start();
                    foreach (LevelWr selectedLevelItem in selectedLevelItems)
                        elementIds = CopyingMepElementsAndConnect(selectedLevelItem.Elevation - bindingLevel.Elevation);
                    int num2 = (int)transaction.Commit();
                }
                if (elementIds == null)
                    return;
                uidoc.Selection.SetElementIds(elementIds);
            }
        }
        private ICollection<ElementId> CopyingMepElementsAndConnect(double offset)
        {
            XYZ translation = (_direction ?? XYZ.BasisZ).Multiply(offset);
            ICollection<ElementId> mepElementsIds = ElementTransformUtils.CopyElements(doc, _mepElWrappers.Select(m => m.Id).ToList(), translation);
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
                MepCurveWr mWr = _mepCurves.FirstOrDefault(m => m.Id == first.IdMepCurve);
                if (mWr != null)
                {
                    Connector opositeConnector = mWr.GetConnectorByDirection(connectorSplitWr3.Direction);
                    Connector connectorByDirection = mWr.GetConnectorByDirection(first.Direction);
                    XYZ nearestEndPoint1 = mWr.GetNearestEndPoint(opositeConnector.Origin);
                    XYZ nearestEndPoint2 = mWr.GetNearestEndPoint(connectorByDirection.Origin);
                    Connector connector = opositeConnector.AllRefs.OfType<Connector>().FirstOrDefault(c => c.Owner.Id != opositeConnector.Owner.Id && c.ConnectorType == ConnectorType.End);
                    mWr.StretchToPoint(nearestEndPoint2, first.Connector.Origin);
                    first.Connector.ConnectTo(opositeConnector);
                    MEPCurve curve = CreateCurve(connectorSplitWr3.Connector.Origin, nearestEndPoint1, mWr);
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
        private MEPCurve CreateCurve(XYZ p1, XYZ p2, MepCurveWr mWr)
        {
            if (p1.DistanceTo(p2) < 0.1)
                return null;
            ElementId id = ElementTransformUtils.CopyElement(doc, mWr.Id, XYZ.Zero).FirstOrDefault();
            MEPCurve element=null;
            int num;
            if (!(id == null))
            {
                element = doc.GetElement(id) as MEPCurve;
                num = element == null ? 1 : 0;
            }
            else
                num = 1;
            if (num != 0)
                return null;
            Curve curve = ((LocationCurve)element.Location).Curve;
            XYZ endpoint1 = p2.DistanceTo(curve.GetEndPoint(0)) > p2.DistanceTo(curve.GetEndPoint(1)) ? p1 : p2;
            XYZ endpoint2 = endpoint1.IsAlmostEqualTo(p1) ? p2 : p1;
            ((LocationCurve)element.Location).Curve = Line.CreateBound(endpoint1, endpoint2);
            return element;
        }
        private List<Tuple<ConnectorSplitWr, ConnectorSplitWr>> GetSplitConnectors(
      IEnumerable<ElementId> mepElementsIds)
        {
            List<ConnectorSplitWr> connectorSplitWrs = new List<ConnectorSplitWr>();
            foreach (ElementId mepElementsId in mepElementsIds)
            {
                Element element = doc.GetElement(mepElementsId);
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
            return idMepCurve != null;
        }
        private bool SameConnector(Connector connector1, Connector connector2)
        {
            if (connector1.Domain != connector2.Domain || connector1.Shape != connector2.Shape || connector1.Shape == ConnectorProfileType.Round && Math.Abs(connector1.Radius - connector2.Radius) > 0.01)
                return false;
            bool flag;
            switch (connector1.Shape)
            {
                case ConnectorProfileType.Rectangular:
                case ConnectorProfileType.Oval:
                    flag = true;
                    break;
                default:
                    flag = false;
                    break;
            }
            return !flag || Math.Abs(connector1.Width - connector2.Width) <= 0.01 && Math.Abs(connector1.Height - connector2.Height) <= 0.01;
        }
    }
}
