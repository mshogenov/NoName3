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
    [ObservableProperty] private bool _isExistingFilterToView;

    public View ActiveView
    {
        get => _activeView;
        set
        {
            _activeView = value;
            OnPropertyChanged();
        }
    }

    private const string _parameterRupture = "msh_Разрыв";


    private readonly List<BuiltInCategory> _categories =
    [
        BuiltInCategory.OST_PipeCurves,
    ];

    public MakeBreakViewModel()
    {
        if (Helpers.CheckParameterExists(_doc, _parameterRupture))
        {
            IsExistingParameter = true;
        }

        if (CheckFilterExists(_doc, "Разрыв"))
        {
            IsExistingFilterInDocument = true;
        }

        if (CheckFamilyExists("Разрыв", out var family))
        {
            IsExistingFamily = true;
        }
        else
        {
            _familySymbol = family;
        }

        if (HasFilterIsActiveView("Разрыв", _activeView))
        {
            _isExistingFilterToView = true;
        }

        uiapp.ViewActivated += OnViewActivated;
    }

    private bool HasFilterIsActiveView(string filterName, View activeView)
    {
        var filter = new FilteredElementCollector(activeView.Document)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>().FirstOrDefault(x => x.Name == filterName);
        ICollection<ElementId> appliedFilters = activeView.GetFilters();
        return appliedFilters.Contains(filter?.Id);
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
        View activeView = e.CurrentActiveView;
        IsExistingFilterToView = HasFilterIsActiveView("Разрыв", activeView);
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
                _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
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
    private void BringBackVisibilityPipe(object param)
    {
        if (param is not Window window) return;
        window.Hide();
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
                window.Show();
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

    [RelayCommand]
    private void MakeBreak(object param)
    {
        if (param is not Window window) return;
        window.Hide();

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
                window.Show();
            }
        });
    }

    [RelayCommand]
    private void DeleteBreak(object param)
    {
        if (param is not Window window) return;
        window.Hide();

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
                window.Show();
            }
        });
    }


    [RelayCommand]
    private void HidePipe(object param)
    {
        if (param is not Window window) return;
        window.Hide();

        _actionEventHandler.Raise(_ =>
        {
            try
            {
                _makeBreakServices.HidePipe(_familySymbol);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + e.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
                window.Show();
            }
        });
    }

    [RelayCommand]
    private void AddFilterToView()
    {
        _actionEventHandler.Raise(_ =>
        {
            using Transaction tr = new Transaction(_doc, "Добавить фильтр к виду");
            try
            {
                tr.Start();
                if (CheckFilterExists(_doc, "Разрыв"))
                {
                    var filter = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Cast<ParameterFilterElement>().FirstOrDefault(x => x.Name == "Разрыв");
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                }
                else
                {
                    var filter = _makeBreakServices.AddFilter(_categories, "Разрыв", "msh_Разрыв", true);
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                }

                IsExistingFilterToView = true;
                tr.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", "Произошла ошибка: " + e.Message);
                tr.RollBack();
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }
}