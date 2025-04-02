using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MarkingOfMarksNoModeless.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using Nice3point.Revit.Toolkit.Options;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace MarkingOfMarksNoModeless.ViewModels
{
    public sealed partial class MarkingOfMarksViewModel : ObservableObject
    {
        public static ActionEventHandler ActionEventHandler { get; set; }
        [ObservableProperty] private bool outstandingFamilyVisibility = false;
        [ObservableProperty] private ObservableCollection<Element> _marks;
        private Element _selectedItem;
        private Document doc;
        DataLoader dataLoader = new("MarkingOfMarksViewModelData");
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

        public MarkingOfMarksViewModel()
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
        }

        private Tuple<List<Element>, List<Element>> SelectElements()
        {
            try
            {
                var selectionConfiguration = new SelectionConfiguration().Allow.Element(e => (e.Category.BuiltInCategory == BuiltInCategory.OST_PipeFitting && (e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба")) || (e.Category.BuiltInCategory == BuiltInCategory.OST_DisplacementElements));
                var elements = Context.ActiveUiDocument.Selection.PickObjects(ObjectType.Element, selectionConfiguration.Filter, "Выберите элементы").Select(x => Context.ActiveDocument.GetElement(x));

                List<Element> selectedElements = [];
                List<Element> displacedElements = [];

                foreach (var element in elements)
                {
                    if (element.Category.BuiltInCategory == BuiltInCategory.OST_DisplacementElements)

                    {
                        displacedElements.Add(element);
                    }
                    else selectedElements.Add(element);

                }


                return new Tuple<List<Element>, List<Element>>(selectedElements, displacedElements);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new Tuple<List<Element>, List<Element>>(new List<Element>(), new List<Element>());
            }

            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);

            }

            return null;
        }

        private bool CanPlaceStamps()
        {
            return SelectedItem != null;
        }

        [RelayCommand(CanExecute = nameof(CanPlaceStamps))]
        public void PlaceStamps()
        {
            ActionEventHandler.Raise(_ =>
            {

                try
                {
                    var selectedElements = SelectElements();
                    string paramNameLevelMark = "msh_Отметка уровня";
                    string paramNameFloor = "ADSK_Этаж";
                    using Transaction tr = new(Context.ActiveDocument, "Расстановка марок");
                    tr.Start();
                    if (selectedElements.Item1 != null)

                    {
                        foreach (var el in selectedElements.Item1)
                        {
                            if (el.Location is LocationPoint location)
                            {


                                double elevation = location.Point.Z; // Получение отметки по оси Z

                                // Перевод из внутренней системы координат Revit (футы) в метры
                                string elevationInMeters = Math.Round((elevation.ToMeters()), 3).ToString();
                                elevationInMeters = elevationInMeters.Replace(',', '.');

                                // Преобразуем строку в число с плавающей точкой
                                if (double.TryParse(elevationInMeters, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                                {
                                    string formattedNumber = "";
                                    // Форматируем число с нужной точностью и добавляем знак плюс
                                    if (elevation >= 0)
                                    {
                                        formattedNumber = number.ToString("+0.000", CultureInfo.InvariantCulture);
                                    }
                                    else formattedNumber = number.ToString("0.000", CultureInfo.InvariantCulture);

                                    // Установка значения в пользовательский параметр
                                    Parameter paramLevelMark = el.LookupParameter(paramNameLevelMark);
                                    paramLevelMark?.Set(formattedNumber);

                                    Parameter paramFloor = el.LookupParameter(paramNameFloor);
                                    if (IsChecked)
                                    {
                                        string levelName = el.FindParameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsValueString();

                                        paramFloor?.Set(levelName);
                                    }


                                    if (!IsChecked && paramFloor.AsValueString() != null)
                                    {
                                        paramFloor.Set((string)null);
                                    }

                                }
                                LocationPoint locPoint = el.Location as LocationPoint;
                                XYZ point = locPoint.Point;
                                XYZ newPoint = new(point.X + 1, point.Y, point.Z);
                                IndependentTag newTag = IndependentTag.Create(Context.ActiveDocument, SelectedItem.Id, Context.ActiveView.Id, new Reference(el), false, TagOrientation.Horizontal, newPoint);
                                if (newTag != null)
                                {
                                    newTag.TagHeadPosition = newPoint;
                                }
                            }
                        }
                    }
                    if (selectedElements.Item2 != null)
                    {
                        foreach (var el in selectedElements.Item2)
                        {

                            DisplacementElement displacementElement = el as DisplacementElement;
                            List<Element> SelectedElementsDisplaced = [];
                            var displacedElementIds = displacementElement.GetDisplacedElementIds();
                            foreach (var elementId in displacedElementIds)
                            {
                                var displacedElementFamily = Context.ActiveDocument.GetElement(elementId);
                                if (displacedElementFamily.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба" && displacedElementFamily is FamilyInstance)
                                {
                                    SelectedElementsDisplaced.Add(displacedElementFamily);

                                }
                            }

                            XYZ pointDisplaced = displacementElement.GetRelativeDisplacement();
                            foreach (var elemDisplaced in SelectedElementsDisplaced)
                            {
                                if (elemDisplaced.Location is LocationPoint location)
                                {
                                    double elevation = location.Point.Z; // Получение отметки по оси Z

                                    // Перевод из внутренней системы координат Revit (футы) в метры
                                    string elevationInMeters = Math.Round((elevation.ToMeters()), 3).ToString();
                                    elevationInMeters = elevationInMeters.Replace(',', '.');

                                    // Преобразуем строку в число с плавающей точкой
                                    if (double.TryParse(elevationInMeters, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                                    {
                                        string formattedNumber = "";
                                        // Форматируем число с нужной точностью и добавляем знак плюс
                                        if (elevation > 0)
                                        {
                                            formattedNumber = number.ToString("+0.000", CultureInfo.InvariantCulture);
                                        }
                                        else formattedNumber = number.ToString("0.000", CultureInfo.InvariantCulture);
                                        // Установка значения в пользовательский параметр
                                        Parameter paramLevelMark = el.LookupParameter(paramNameLevelMark);
                                        paramLevelMark?.Set(formattedNumber);

                                        Parameter paramFloor = el.LookupParameter(paramNameFloor);
                                        if (IsChecked)
                                        {
                                            string levelName = el.FindParameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsValueString();

                                            paramFloor?.Set(levelName);
                                        }
                                        if (!IsChecked && paramFloor?.AsValueString() != null)
                                        {
                                            paramFloor.Set((string)null);
                                        }
                                    }

                                    XYZ point = location.Point;
                                    XYZ newPoint = new(point.X + pointDisplaced.X + 1, point.Y + pointDisplaced.Y, point.Z + pointDisplaced.Z);
                                    IndependentTag newTag = IndependentTag.Create(Context.ActiveDocument, SelectedItem.Id, Context.ActiveDocument.ActiveView.Id, new Reference(elemDisplaced), false, TagOrientation.Horizontal, newPoint);
                                    if (newTag != null)
                                    {
                                        newTag.TagHeadPosition = newPoint;
                                    }

                                }
                            }
                        }

                    }
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
                }

            });



        }

        [RelayCommand]
        public void UpdateMarks(Window window)
        {
            string paramNameLevelMark = "msh_Отметка уровня";
            string paramNameFloor = "ADSK_Этаж";
            var elems = new FilteredElementCollector(Context.ActiveDocument).OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType().Where(x => x.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба");
            using Transaction tr = new(Context.ActiveDocument, "Обновление марок");
            tr.Start();
            {
                foreach (var el in elems)
                {
                    if (el.Location is LocationPoint location)
                    {


                        double elevation = location.Point.Z; // Получение отметки по оси Z

                        // Перевод из внутренней системы координат Revit (футы) в метры
                        string elevationInMeters = Math.Round((elevation.ToMeters()), 3).ToString();
                        elevationInMeters = elevationInMeters.Replace(',', '.');

                        // Преобразуем строку в число с плавающей точкой
                        if (double.TryParse(elevationInMeters, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                        {
                            string formattedNumber = "";
                            // Форматируем число с нужной точностью и добавляем знак плюс
                            if (elevation >= 0)
                            {
                                formattedNumber = number.ToString("+0.000", CultureInfo.InvariantCulture);
                            }
                            else formattedNumber = number.ToString("0.000", CultureInfo.InvariantCulture);

                            // Установка значения в пользовательский параметр
                            Parameter paramLevelMark = el.LookupParameter(paramNameLevelMark);
                            paramLevelMark?.Set(formattedNumber);
                            Parameter paramFloor = el.LookupParameter(paramNameFloor);
                            if (IsChecked)
                            {
                                string levelName = el.FindParameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsValueString();

                                paramFloor?.Set(levelName);
                            }
                            if (!IsChecked && paramFloor.AsValueString() != null)
                            {
                                paramFloor.Set((string)null);
                            }

                        }

                    }
                }
            }
            tr.Commit();
            dataLoader.SaveData(IsChecked);
            window.Close();

        }
        [RelayCommand]
        private void DownloadFamily()
        {
            string familyFilePath = @"C:\Users\User\AppData\Roaming\NoNameData\MarkingOfMarks\Высотные отметки.rfa";
            ActionEventHandler.Raise(_ =>
{
    try
    {
        using Transaction trans = new(doc, "Загрузить семейство");
        trans.Start();
        doc.LoadFamily(familyFilePath, out Family family);
        if (family != null)
        {
            TaskDialog.Show("Успешно", "Семейство было успешно загружено.");
        }
        else
        {
            TaskDialog.Show("Ошибка", "Не удалось загрузить семейство.");
        }
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


        }
    }

}
