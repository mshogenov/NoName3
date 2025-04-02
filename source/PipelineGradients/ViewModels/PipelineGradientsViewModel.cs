using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.DependencyInjection;
using PipelineGradients.Models;
using System.Windows.Controls;

namespace PipelineGradients.ViewModels
{
    public sealed partial class PipelineGradientsViewModel : ObservableObject
    {
        private List<PipeMdl> pipesMdl = [];
       [ObservableProperty] private List<MarkAnnotationMdl> _markAnnotationMdls = [];
        public View ActiveView { get; set; }
        public List<Element> PipeAnnotations { get; } = [];
        public PipelineGradientsViewModel() 
        {
            UIDocument uidoc = Context.ActiveUiDocument;
            Document doc = uidoc.Document;
            ActiveView = uidoc.ActiveView;
            // Устанавливаем фильтр для труб
            ElementCategoryFilter pipesFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);

            // Получаем все элементы на активном виде, которые соответствуют фильтру
            FilteredElementCollector collector = new FilteredElementCollector(doc, ActiveView.Id).WhereElementIsNotElementType();
            var pipesOnView = collector.WherePasses(pipesFilter).Cast<Pipe>();
            foreach (Element pipeOnView in pipesOnView) 
            {
                pipesMdl.Add(new PipeMdl(pipeOnView));
            }
            FilteredElementCollector collectortags = new FilteredElementCollector(doc);
            collectortags.OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_PipeTags).WhereElementIsElementType();

            foreach (var element in collectortags)
            {
                MarkAnnotationMdls.Add(new MarkAnnotationMdl(element));
            }



        }
        

    }
}