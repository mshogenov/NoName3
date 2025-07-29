using System.Windows;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using MakeBreak.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Utils;

namespace MakeBreak.ViewModels;

public sealed partial class MakeBreakViewModel : ObservableObject
{
    private readonly ActionEventHandler _actionEventHandler = new();
    private readonly MakeBreakServices _makeBreakServices = new();
    private readonly FamilySymbol _familySymbol;
    private readonly UIApplication uiApp = Context.UiApplication;
    private readonly Document _doc = Context.ActiveDocument;
    private readonly View _activeView = Context.ActiveView;
    [ObservableProperty] private bool _isExistingFilterBreak_3D;
    [ObservableProperty] private bool _isExistingFilterBreak_Plan;
    [ObservableProperty] private bool _isExistingParameter_msh_Break_3D;
    [ObservableProperty] private bool _isExistingParameter_msh_Break_Plan;
    [ObservableProperty] private bool _isExistingFamily;
    [ObservableProperty] private bool _isExistingFilterToViewBreak_3D = true;
    [ObservableProperty] private bool _isExistingFilterToViewBreak_Plan = true;

    private const string _parameterName_msh_Break_3D = "msh_Разрыв_3D";
    private const string _parameterName_msh_Break_Plan = "msh_Разрыв_План";
    private const string _filterName_Break_Plan = "Разрыв_План";
    private const string _filterName_Break_3D = "Разрыв_3D";

    private readonly List<BuiltInCategory> _categories =
    [
        BuiltInCategory.OST_PipeCurves,
    ];

    public MakeBreakViewModel()
    {
        if (Helpers.CheckParameterExists(_doc, _parameterName_msh_Break_3D))
        {
            _isExistingParameter_msh_Break_3D = true;
        }

        if (Helpers.CheckParameterExists(_doc, _parameterName_msh_Break_Plan))
        {
            _isExistingParameter_msh_Break_Plan = true;
        }


        if (CheckFilterExists(_doc, _filterName_Break_3D))
        {
            _isExistingFilterBreak_3D = true;
        }

        if (CheckFilterExists(_doc, _filterName_Break_Plan))
        {
            _isExistingFilterBreak_Plan = true;
        }

        if (CheckFamilyExists("Разрыв", out var family))
        {
            IsExistingFamily = true;
            _familySymbol = family;
        }

        switch (_activeView.ViewType)
        {
            case ViewType.ThreeD:
                _isExistingFilterToViewBreak_3D = HasFilterIsActiveView(_filterName_Break_3D, _activeView);
                break;
            case ViewType.FloorPlan:
                _isExistingFilterToViewBreak_Plan = HasFilterIsActiveView(_filterName_Break_Plan, _activeView);
                break;
        }


        uiApp.ViewActivated += OnViewActivated;
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
        switch (activeView.ViewType)
        {
            case ViewType.ThreeD:
                IsExistingFilterToViewBreak_3D = HasFilterIsActiveView(_filterName_Break_3D, activeView);
                IsExistingFilterBreak_Plan = true;
                break;
            case ViewType.FloorPlan:
                IsExistingFilterBreak_Plan = HasFilterIsActiveView(_filterName_Break_Plan, activeView);
                IsExistingFilterToViewBreak_3D = true;
                break;
        }
    }

    [RelayCommand]
    private void AddFilter_Break_3D()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Добавить фильтр вида");
                tr.Start();
                var filter = _makeBreakServices.AddFilter(_categories, _filterName_Break_3D,
                    _parameterName_msh_Break_3D, true);
                if (_activeView.ViewType == ViewType.ThreeD)
                {
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                    IsExistingFilterToViewBreak_3D = true;
                }

                tr.Commit();
                IsExistingFilterBreak_3D = true;
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
    private void AddFilter_Break_Plan()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Добавить фильтр вида");
                tr.Start();
                var filter = _makeBreakServices.AddFilter(_categories, _filterName_Break_Plan,
                    _parameterName_msh_Break_Plan, true);
                if (_activeView.ViewType == ViewType.FloorPlan)
                {
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                    IsExistingFilterToViewBreak_Plan = true;
                }

                tr.Commit();
                IsExistingFilterBreak_Plan = true;
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
                IsExistingFamily = true;
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
    private void AddParameter_msh_Break_3D()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Добавить общий параметр");
                tr.Start();
                var isCreate = Helpers.CreateSharedParameter(_doc, _parameterName_msh_Break_3D,
                    SpecTypeId.Boolean.YesNo, GroupTypeId.Graphics, true, _categories);
                tr.Commit();
                if (isCreate)
                {
                    IsExistingParameter_msh_Break_3D = true;
                }
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
    private void AddParameter_msh_Break_Plan()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                using Transaction tr = new Transaction(_doc, "Добавить общий параметр");
                tr.Start();
                var isCreate = Helpers.CreateSharedParameter(_doc, _parameterName_msh_Break_Plan,
                    SpecTypeId.Boolean.YesNo, GroupTypeId.Graphics, true, _categories);
                tr.Commit();
                if (isCreate)
                {
                    IsExistingParameter_msh_Break_Plan = true;
                }
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
    private void AddFilterViewBreak_3DToView()
    {
        _actionEventHandler.Raise(_ =>
        {
            using Transaction tr = new Transaction(_doc, "Добавить фильтр к виду");
            try
            {
                tr.Start();
                if (CheckFilterExists(_doc, _filterName_Break_3D))
                {
                    var filter = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Cast<ParameterFilterElement>().FirstOrDefault(x => x.Name == _filterName_Break_3D);
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                    IsExistingFilterBreak_3D = true;
                }
                else
                {
                    var filter = _makeBreakServices.AddFilter(_categories, _filterName_Break_3D,
                        _parameterName_msh_Break_3D, true);
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                }

                IsExistingFilterToViewBreak_3D = true;
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

    [RelayCommand]
    private void AddFilterViewBreak_PlanToView()
    {
        _actionEventHandler.Raise(_ =>
        {
            using Transaction tr = new Transaction(_doc, "Добавить фильтр к виду");
            try
            {
                tr.Start();
                if (CheckFilterExists(_doc, _filterName_Break_Plan))
                {
                    var filter = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Cast<ParameterFilterElement>().FirstOrDefault(x => x.Name == _filterName_Break_Plan);
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                    IsExistingFilterBreak_Plan = true;
                }
                else
                {
                    var filter = _makeBreakServices.AddFilter(_categories, _filterName_Break_Plan,
                        _parameterName_msh_Break_Plan, true);
                    _makeBreakServices.ApplyFilterToView(_activeView, filter, false);
                }

                IsExistingFilterToViewBreak_Plan = true;
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