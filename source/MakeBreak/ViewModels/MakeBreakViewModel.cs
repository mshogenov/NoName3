using System.Windows;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using MakeBreak.Commands;
using MakeBreak.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Utils;

namespace MakeBreak.ViewModels;

public sealed partial class MakeBreakViewModel : ObservableObject
{
    private readonly ActionEventHandler _actionEventHandler = new();
    private readonly MakeBreakServices _makeBreakServices = new();
    private readonly FamilySymbol _familySymbol;
    private UIApplication uiapp = Context.UiApplication;
    private readonly Document _doc = Context.ActiveDocument;
    private View _activeView = Context.ActiveView;
    [ObservableProperty] private bool _isExistingFilterInDocument;
    [ObservableProperty] private bool _isExistingParameter;
    [ObservableProperty] private bool _isExistingFamily;

    public View ActiveView
    {
        get => _activeView;
        set
        {
            _activeView = value;
            OnPropertyChanged(nameof(ActiveView));
        }
    }

    private const string _parameterRupture = "msh_Разрыв";

    // Добавьте поле для отслеживания состояния выполнения команды
    private bool _isExecutingMakeBreak;


    private readonly List<BuiltInCategory> _categories =
    [
        BuiltInCategory.OST_PipeCurves,
    ];

    private bool _isExecutingDeleteBreak;


    public MakeBreakViewModel()
    {
        if (!Helpers.CheckParameterExists(_doc, _parameterRupture))
        {
            IsExistingParameter = true;
        }

        if (!CheckFilterExists(_doc, "Разрыв"))
        {
            IsExistingFilterInDocument = true;
        }

        if (!CheckFamilyExists("Разрыв", out var family))
        {
            IsExistingFamily = true;
        }
        else
        {
            _familySymbol = family;
        }

        if (_activeView.ViewType is ViewType.ThreeD or ViewType.FloorPlan)
        {
            var filter = new FilteredElementCollector(_doc)
                .OfClass(typeof(ParameterFilterElement))
                .Cast<ParameterFilterElement>().FirstOrDefault(x => x.Name == "Разрыв");
            // проверка фильтра на правильность настроек

            ICollection<ElementId> appliedFilters = _activeView.GetFilters();
        }


        uiapp.ViewActivated += OnViewActivated;
    }

    private bool CheckFamilyExists(string familyName, out FamilySymbol family)
    {
        family = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .OfClass(typeof(FamilySymbol)).FirstOrDefault(x => x.Name == familyName) as FamilySymbol;
        return family != null;
    }


    public bool CheckFilterExists(Document doc, string filterName)
    {
        var filter = new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>().FirstOrDefault(x => x.Name == filterName);
        return filter != null;
        // // Проверяем категории фильтра
        // var missingCategories = GetMissingCategories(filter, _categories);
        // if (missingCategories.Count != 0)
        // {
        //     // Получаем текущие категории фильтра
        //     var currentCategories = filter.GetCategories().ToList();
        //     currentCategories.AddRange(missingCategories.Select(category => new ElementId((long)category)));
        //
        //     // Обновляем категории фильтра
        //     filter.SetCategories(currentCategories);
        // }
        //
        // // Проверяем правила фильтра
        // ParameterElement paramElement = GetParameterElement("msh_Разрыв");
        // var elementFilter = filter.GetElementFilter() as LogicalAndFilter;
        // var filters = elementFilter.GetFilters().Select(x => x as ElementParameterFilter);
        // foreach (var f in filters)
        // {
        //     var rule = f.GetRules().FirstOrDefault();
        //     switch (rule)
        //     {
        //         case null:
        //             return false;
        //         case FilterIntegerRule integerRule:
        //         {
        //             var param = integerRule.GetRuleParameter();
        //             if (!(paramElement != null && param.Equals(paramElement.Id)))
        //             {
        //                 return false;
        //             }
        //             break;
        //         }
        //     }
        // }
        //
    }

    private ParameterElement GetParameterElement(string parameterName)
    {
        return new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterElement))
            .Cast<ParameterElement>()
            .FirstOrDefault(pe => pe.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
    }

    private IList<BuiltInCategory> GetMissingCategories(ParameterFilterElement filter, List<BuiltInCategory> categories)
    {
        var filterCategoryIds = filter.GetCategories()
            .Select(id => id.Value)
            .ToList();

        // Получаем категории, которых нет в фильтре
        var missingCategories = categories
            .Where(category => !filterCategoryIds.Contains((int)category))
            .ToList();

        return missingCategories;
    }

    private void OnViewActivated(object sender, ViewActivatedEventArgs e)
    {
        Document doc = e.Document;
        View activeView = e.CurrentActiveView;
        // проверка наличия фильтра на виде
    }

    [RelayCommand]
    private void AddFilter()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Добавить фильтр вида");
                tr.Start();
                var filter = _makeBreakServices.AddFilter(_categories, "Разрыв", "msh_Разрыв", true);
                _makeBreakServices.ApplyFilterToView(_activeView, filter);
                tr.Commit();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand]
    private void AddFamily()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Загрузить семейство");
                tr.Start();
                _makeBreakServices.DownloadFamily("Разрыв");
               tr.Commit();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand]
    private void BringBackVisibilityPipe()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                _makeBreakServices.BringBackVisibilityPipe(_familySymbol);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand]
    private void AddParameter()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Добавить общий параметр");
                tr.Start();
                var isCreate = Helpers.CreateSharedParameter(_doc, _parameterRupture,
                    SpecTypeId.Boolean.YesNo, GroupTypeId.Graphics, true, _categories);
                tr.Commit();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand(CanExecute = nameof(CanExecuteMakeBreak))]
    private void MakeBreak()
    {
        // Устанавливаем флаг, что команда запущена
        _isExecutingMakeBreak = true;
        // Уведомляем об изменении состояния для обновления доступности команды
        (MakeBreakCommand as RelayCommand)?.NotifyCanExecuteChanged();
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                if (_familySymbol != null)
                {
                    _makeBreakServices.CreateTwoCouplingsAndSetMidPipeParameter(_familySymbol);
                }
                else
                {
                    TaskDialog.Show("Информация", "В проекте отсутствует семейство: \"Разрыв\"");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + ex.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
                // Сбрасываем флаг после завершения команды
                _isExecutingMakeBreak = false;

                // Уведомляем об изменении состояния
                (MakeBreakCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        });
    }

    // Метод для проверки возможности выполнения команды
    private bool CanExecuteMakeBreak() => !_isExecutingMakeBreak;

    [RelayCommand(CanExecute = nameof(CanExecuteDeleteBreak))]
    private void DeleteBreak()
    {
        // Устанавливаем флаг, что команда запущена
        _isExecutingDeleteBreak = true;
        (DeleteBreakCommand as RelayCommand)?.NotifyCanExecuteChanged();
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                _makeBreakServices.DeleteBreaks(_familySymbol);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + e.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
                // Сбрасываем флаг после завершения команды
                _isExecutingDeleteBreak = false;

                // Уведомляем об изменении состояния
                (DeleteBreakCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        });
    }

    private bool CanExecuteDeleteBreak() => !_isExecutingDeleteBreak;
}