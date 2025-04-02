using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using DesignationOfRisers.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nice3point.Revit.Extensions;
using Nice3point.Revit.Toolkit;
using NoNameApi.Utils;

namespace DesignationOfRisers.ViewModels
{
    public sealed partial class DesignationOfRisersViewModel : ObservableObject
    {
        [ObservableProperty] private List<ViewMdl> _viewsMdl = [];
       
        [ObservableProperty] private ObservableCollection<PipingSystemMdl> _pipingSystemMdls = [];
        [ObservableProperty] private List<ViewMdl> _selectedViews = [];
        private readonly DataLoader dataLoader = new DataLoader();
        [ObservableProperty] private View _activeView;
        [ObservableProperty] private bool _isCheckedActiveView=true;
        [ObservableProperty] private bool _isCheckedSelectedView;
        private IEnumerable<IGrouping<Element, Pipe>> risers;
        private Document doc;
        public DesignationOfRisersViewModel()
        {
            doc = Context.ActiveDocument;
            _activeView = Context.ActiveView;
            var views = GetAllViews(doc).OrderBy(x=>x.Name);
            foreach (var view in views)
            {
                _viewsMdl.Add(new ViewMdl(view));
            }
            var pipeSystemTypes = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipingSystem).WhereElementIsElementType().Cast<PipingSystemType>().
                Where(x => x.FindParameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM).AsValueString() != "").OrderBy(x=>x.Name);

            foreach (var pipeSystem in pipeSystemTypes)
            {
                _pipingSystemMdls.Add(new PipingSystemMdl(pipeSystem));
            }
            var marks = new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)).Cast<IndependentTag>();

            risers = Helpers.GetAllRisers(doc, 1, 1600);
            // Загрузим существующие данные и обновим коллекцию
            PipingSystemMdls = dataLoader.LoadDataWithSync(PipingSystemMdls, doc) ?? new ObservableCollection<PipingSystemMdl>();

            
        }
       
        [RelayCommand]
        public void MarkRisers(Window window)
        {
            using (Transaction tr = new(doc, "Обозначение стояков"))
            {
                tr.Start();
                if (IsCheckedSelectedView)
                {

                    foreach (var view in ViewsMdl)
                    {
                        if (view.IsCheked)
                        {
                            foreach (var pipingSystem in PipingSystemMdls)
                            {
                                if (pipingSystem.IsChecked)
                                {
                                    foreach (var riser in risers)
                                    {
                                        foreach (var pipe in riser)
                                        {
                                            ElementCategoryFilter pipesFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);
                                            FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType();
                                            var pipesOnView = collector.WherePasses(pipesFilter).Cast<Pipe>().Where(x => x.FindParameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString() == pipingSystem.Name);
                                            foreach (var pipeOnView in pipesOnView)
                                            {
                                                if (pipe.Id == pipeOnView.Id)
                                                {
                                                    // Получаем местоположение трубы
                                                    LocationCurve locCurve = pipe.Location as LocationCurve;
                                                    XYZ point = locCurve.Curve.GetEndPoint(0); // Используем начальную точку кривой как место для марки

                                                    // Создаем экземпляр марки
                                                    IndependentTag tag = IndependentTag.Create(doc, view.Id, new Reference(pipe), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, point);

                                                    // Назначаем тип марки
                                                    tag.ChangeTypeId(pipingSystem.SelectedMark.Id);

                                                }
                                            }


                                        }
                                    }

                                }
                            }


                        }
                    }
                }
                if (IsCheckedActiveView) 
                {
                    foreach (var pipingSystem in PipingSystemMdls)
                    {
                        if (pipingSystem.IsChecked)
                        {
                            foreach (var riser in risers)
                            {
                                foreach (var pipe in riser)
                                {
                                    ElementCategoryFilter pipesFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);
                                    FilteredElementCollector collector = new FilteredElementCollector(doc, ActiveView.Id).WhereElementIsNotElementType();
                                    var pipesOnView = collector.WherePasses(pipesFilter).Cast<Pipe>().Where(x => x.FindParameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString() == pipingSystem.Name);
                                    foreach (var pipeOnView in pipesOnView)
                                    {
                                        if (pipe.Id == pipeOnView.Id)
                                        {
                                            // Получаем местоположение трубы
                                            LocationCurve locCurve = pipe.Location as LocationCurve;
                                            XYZ point = locCurve.Curve.GetEndPoint(0); // Используем начальную точку кривой как место для марки

                                            // Создаем экземпляр марки
                                            IndependentTag tag = IndependentTag.Create(doc, ActiveView.Id, new Reference(pipe), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, point);

                                            // Назначаем тип марки
                                            tag.ChangeTypeId(pipingSystem.SelectedMark.Id);

                                        }
                                    }


                                }
                            }

                        }
                    }

                }
               
                tr.Commit();
                window?.Close();
                dataLoader.SaveData(PipingSystemMdls);

            }
        }
       
        public static List<View> GetAllViews(Document doc)
        {
            // Создаем фильтр для выбора всех объектов типа View
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(View));

            // Создаем список для хранения видов
            List<View> views = new List<View>();

            // Проходимся по всем элементам и добавляем только те, которые не являются шаблонами видов
            foreach (Element element in collector)
            {
                View view = element as View;
                if (view != null && !view.IsTemplate && view.ViewType == ViewType.FloorPlan)
                {
                    views.Add(view);
                }
            }

            return views;
        }

    }
}