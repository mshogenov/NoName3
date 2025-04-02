using Autodesk.Revit.UI;
using CopyByLevel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyByLevel.Services
{
    public class BaseService
    {
        protected BaseService()
        {
            UiDocument = Context.UiApplication.ActiveUIDocument;
            Document = UiDocument.Document;
            MepElemSelectionFilter = new MepElemSelectionFilter();

        }

        protected UIDocument UiDocument { get; }

        protected Document Document { get; }

        protected FilteredElementCollector Collector => new FilteredElementCollector(Document);

        protected MepElemSelectionFilter MepElemSelectionFilter { get; }


    }
}
