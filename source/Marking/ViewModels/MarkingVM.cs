using Autodesk.Revit.UI;
using Marking.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using System.Collections.ObjectModel;
using System.Windows;

namespace Marking.ViewModels
{
    public sealed partial class MarkingVM : ObservableObject
    {
        public static ActionEventHandler ActionEventHandler { get; set; }
        [ObservableProperty] private bool outstandingFamilyVisibility = false;
        [ObservableProperty] private ObservableCollection<Element> _marks;
        private Element _selectedItem;
        private Document doc;
        DataLoader dataLoader = new("MarkingData");
        private MarkingServices markingServices;
        public Element SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                PlaceStampsCommand.NotifyCanExecuteChanged();
            }
        }
        [ObservableProperty][NotifyCanExecuteChangedFor(nameof(PlaceStampsCommand))] private bool _isChecked;

        public MarkingVM()
        {
            doc = Context.ActiveDocument;
            ActionEventHandler = new ActionEventHandler();
            var marks = new FilteredElementCollector(Context.ActiveDocument)
                                    .OfClass(typeof(FamilySymbol))
                                    .Where(x => (x as FamilySymbol).Family.Name == "Высотные отметки")
                                    .OrderBy(e => e.Name)
                                    .ToList();
            _marks = new ObservableCollection<Element>(marks);
            if (_marks.Count == 0)
            {
                OutstandingFamilyVisibility = true;
            }
            IsChecked = dataLoader.LoadData<bool>();
            markingServices = new MarkingServices();
        }

        private bool CanPlaceStamps()
        {
            return SelectedItem != null;
        }
        [RelayCommand(CanExecute = nameof(CanPlaceStamps))]
        public void PlaceStamps(Window window)
        {
            window?.Hide();
            var selectedElements = markingServices.SelectElements();
            ActionEventHandler.Raise(_ =>
            {
                try
                {
                    using Transaction tr = new(Context.ActiveDocument, "Расстановка марок");
                    tr.Start();
                    markingServices.PlaceStamps(selectedElements,SelectedItem,IsChecked);
                    tr.Commit();
                    dataLoader.SaveData(IsChecked);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                }
                finally
                {
                    ActionEventHandler.Cancel();
                    window?.Show();
                }
            });
        }
        [RelayCommand]
        public void UpdateMarks()
        {
            var elems = new FilteredElementCollector(Context.ActiveDocument)
                .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType()
                .Where(x => x.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба");
            ActionEventHandler.Raise(_ =>
            {
                try
                {
                    using Transaction tr = new(Context.ActiveDocument, "Обновление марок");
                    tr.Start();
                    {
                        foreach (var el in elems)
                        {
                            markingServices.HeightSetting(el, IsChecked);
                        }
                    }
                    tr.Commit();
                }
                catch (Exception ex) 
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                }
                finally
                {
                    ActionEventHandler.Cancel();
                }
            });
           
            dataLoader.SaveData(IsChecked);
          
        }
        [RelayCommand]
        private void DownloadFamily()
        {

            ActionEventHandler.Raise(_ =>
{
    try
    {
        using Transaction trans = new(doc, "Загрузить семейство");
        trans.Start();

      markingServices.DownloadFamily(doc,"Высотные отметки");
        trans.Commit();
    }
    catch (Exception ex)
    {
        TaskDialog.Show("Ошибка", ex.Message);
    }
    finally
    {
        ActionEventHandler.Cancel();
    }
});
            var marks = new FilteredElementCollector(Context.ActiveDocument)
                                 .OfClass(typeof(FamilySymbol))
                                 .Where(x => (x as FamilySymbol).Family.Name == "Высотные отметки")
                                 .OrderBy(e => e.Name)
                                 .ToList();
            Marks = new ObservableCollection<Element>(marks);
            if (Marks.Count != 0)
            {
                OutstandingFamilyVisibility = false;
            }

        }
    }

}
