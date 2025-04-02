using System.Windows;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SystemModelingCommands.Filters;
using SystemModelingCommands.Services;
using SystemModelingCommands.Views;

namespace SystemModelingCommands.ViewModels
{
    public partial class BloomViewModel : ObservableObject
    {
        [ObservableProperty] private List<MEPCurveType> _mepCurveTypes = [];
        [ObservableProperty] private MEPCurveType _selectedMepCurveType;
        [ObservableProperty] private string _message;

        public BloomViewModel()
        {
            var doc = Context.ActiveDocument;
            var uidoc = Context.ActiveUiDocument;
            // Создаем фильтр для проверки
            FittingSelectionFilter filter = new FittingSelectionFilter();
            Element selectedElement = null;
            // Проверка, есть ли выбранный элемент до запуска скрипта
            var selectedIds = Context.ActiveUiDocument?.Selection.GetElementIds();
            try
            {
                if (selectedIds is { Count: 1 })
                {
                    // Получить первый выбранный элемент
                    if (doc != null)
                    {
                        Element preSelectedElement = doc.GetElement(selectedIds.First());
                        // Проверка, проходит ли выбранный элемент фильтр
                        if (filter.AllowElement(preSelectedElement))
                        {
                            selectedElement = preSelectedElement;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
            }

            // Если элемент не был предварительно выбран или не соответствует фильтру, запустить выбор элемента пользователем
            if (selectedElement == null)
            {
                try
                {
                    Reference reference = uidoc?.Selection.PickObject(ObjectType.Element, filter);
                    selectedElement = doc?.GetElement(reference);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return; // Пользователь отменил выбор
                }
            }

            Connector[] source = SystemModelingServices.ConnectorArrayUnused(selectedElement);
            switch (selectedElement?.Category.BuiltInCategory)
            {
                case BuiltInCategory.OST_DuctFitting:
                    var ductTypes = new FilteredElementCollector(doc).OfClass(typeof(DuctType))
                        .WhereElementIsElementType()
                        .Cast<MEPCurveType>().OrderBy(x => x.Name).ToList();
                    foreach (var connector in source)
                    {
                        if (connector.Shape == ConnectorProfileType.Round)
                        {
                            _mepCurveTypes.AddRange(ductTypes.Where(d =>
                                d.FamilyName == "Воздуховод круглого сечения"));
                            break;
                        }

                        if (connector.Shape == ConnectorProfileType.Oval)
                        {
                            _mepCurveTypes.AddRange(ductTypes.Where(d =>
                                d.FamilyName == "Воздуховод круглого сечения"));
                            break;
                        }

                        if (connector.Shape == ConnectorProfileType.Rectangular)
                        {
                            _mepCurveTypes.AddRange(ductTypes.Where(d =>
                                d.FamilyName == "Воздуховод прямоугольного сечения"));
                            break;
                        }
                    }

                    break;
                case BuiltInCategory.OST_PipeFitting:
                    _mepCurveTypes = new FilteredElementCollector(doc).OfClass(typeof(PipeType))
                        .WhereElementIsElementType()
                        .Cast<MEPCurveType>().OrderBy(x => x.Name).ToList();
                    break;
            }


            using Transaction transaction = new(doc, "Расширение");
            MEPCurveType pipeType = SystemModelingServices.DeterminingTypeOfPipeByFitting(doc, selectedElement);

            if (pipeType == null)
            {
                BloomView view = new BloomView(this);
                view.ShowDialog();
                if (SelectedMepCurveType != null)
                {
                    pipeType = SelectedMepCurveType;
                }
                else
                {
                    return;
                }
            }

            transaction.Start();
            try
            {
                SystemModelingServices.Bloom(doc, selectedElement, pipeType);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
            }

            transaction.Commit();
        }

        [RelayCommand]
        private void ExecuteCheckAndClose(object parameter)
        {
            if (SelectedMepCurveType == null)
            {
                Message = "Пожалуйста, выберите тип трубы из списка.";
            }
            else
            {
                if (parameter is Window window)
                {
                    window.Close();
                }
            }
        }
    }
}