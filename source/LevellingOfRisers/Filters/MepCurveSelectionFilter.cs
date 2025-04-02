using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace LevellingOfRisers.Filters
{
    internal class MepCurveSelectionFilter : ISelectionFilter
    {
        public bool FreeConnectors { get; set; }

        public bool AllowElement(Element elem) => elem is MEPCurve && !(elem is FlexDuct) && !(elem is FlexPipe) && !(elem is InsulationLiningBase) && (!FreeConnectors || !IsNotFreeConnector(elem));

        public bool AllowReference(Reference reference, XYZ position) => false;

        private bool IsNotFreeConnector(Element mepElement) => ((MEPCurve)mepElement).ConnectorManager.UnusedConnectors.Size == 0;
    }
}
