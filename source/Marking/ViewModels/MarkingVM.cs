using Autodesk.Revit.UI;
using Marking.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using System.Collections.ObjectModel;
using System.Windows;
using Marking.Models;
using NoNameApi.Services;

namespace Marking.ViewModels
{
    public sealed partial class MarkingVM : ObservableObject
    {
        private readonly ActionEventHandler _actionEventHandler = new();
        [ObservableProperty] private bool _outstandingFamilyVisibility;
        [ObservableProperty] private ObservableCollection<Element> _marks;

        private readonly Document _doc = Context.ActiveDocument;
        private readonly JsonDataLoader _dataLoader = new("MarkingData");
        private readonly MarkingServices _markingServices = new();
        private readonly MarkingDTO _markingDTO = new();
        private Element _selectedMark;

        public Element SelectedMark
        {
            get => _selectedMark;
            set
            {
                SetProperty(ref _selectedMark, value);
                PlaceStampsCommand.NotifyCanExecuteChanged();
            }
        }

        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(PlaceStampsCommand))]
        private bool _recordFloorIsChecked;

        public MarkingVM()
        {
            var marks = new FilteredElementCollector(Context.ActiveDocument)
                .OfClass(typeof(FamilySymbol))
                .Where(x => (x as FamilySymbol)?.Family.Name == "Высотные отметки")
                .OrderBy(e => e.Name)
                .ToList();
            _marks = new ObservableCollection<Element>(marks);
            if (_marks.Count == 0)
            {
                OutstandingFamilyVisibility = true;
            }

            var dataLoader = _dataLoader.LoadData<MarkingDTO>();
            if (dataLoader != null)
            {
                _markingDTO.RecordFloorIsChecked = dataLoader.RecordFloorIsChecked;
                RecordFloorIsChecked = dataLoader.RecordFloorIsChecked;
            }
            else
            {
                RecordFloorIsChecked = true;
            }
        }

        private bool CanPlaceStamps()
        {
            return SelectedMark != null;
        }

        [RelayCommand(CanExecute = nameof(CanPlaceStamps))]
        private void PlaceStamps(Window window)
        {
            window?.Hide();
            var selectedElements = _markingServices.SelectElements();
            _actionEventHandler.Raise(_ =>
            {
                try
                {
                    using Transaction tr = new(Context.ActiveDocument, "Расстановка марок");
                    tr.Start();
                    _markingServices.PlaceStamps(selectedElements, SelectedMark, RecordFloorIsChecked);
                    tr.Commit();
                    _markingDTO.RecordFloorIsChecked = RecordFloorIsChecked;
                    _dataLoader.SaveData(_markingDTO);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                }
                finally
                {
                    _actionEventHandler.Cancel();
                    window?.Show();
                }
            });
        }

        [RelayCommand]
        private void UpdateMarks()
        {
            var elems = new FilteredElementCollector(Context.ActiveDocument)
                .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType()
                .Where(x => x.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба");
            _actionEventHandler.Raise(_ =>
            {
                try
                {
                    using Transaction tr = new(Context.ActiveDocument, "Обновление марок");
                    tr.Start();
                    {
                        foreach (var el in elems)
                        {
                            MarkingServices.HeightSetting(el, RecordFloorIsChecked);
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
                    _actionEventHandler.Cancel();
                }
            });
            _markingDTO.RecordFloorIsChecked = RecordFloorIsChecked;
            _dataLoader.SaveData(_markingDTO);
        }

        [RelayCommand]
        private void DownloadFamily()
        {
            _actionEventHandler.Raise(_ =>
            {
                try
                {
                    using Transaction trans = new(_doc, "Загрузить семейство");
                    trans.Start();

                    MarkingServices.DownloadFamily(_doc, "Высотные отметки");
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                }
                finally
                {
                    _actionEventHandler.Cancel();
                }
            });
            var marks = new FilteredElementCollector(Context.ActiveDocument)
                .OfClass(typeof(FamilySymbol))
                .Where(x => (x as FamilySymbol)?.Family.Name == "Высотные отметки")
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