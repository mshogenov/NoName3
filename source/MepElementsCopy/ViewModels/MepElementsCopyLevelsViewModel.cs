using System.Globalization;
using System.Windows;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;
using MepElementsCopy.Models;
using MepElementsCopy.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Services;
using NoNameApi.Views;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace MepElementsCopy.ViewModels;

public sealed partial class MepElementsCopyLevelsViewModel : ObservableObject
{
    [ObservableProperty] private List<LevelModel> _selectedLevelModels = [];
    [ObservableProperty] private List<LevelModel> _levelModels = [];
    private LevelModel _selectedLevelModel;

    public LevelModel SelectedLevelModel
    {
        get => _selectedLevelModel;
        set
        {
            if (Equals(value, _selectedLevelModel)) return;
            _selectedLevelModel = value;
            OnPropertyChanged();
        }
    }

    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uiDoc = Context.ActiveUiDocument;
    private readonly ActionEventHandler _actionEventHandler = new();
    private readonly MepElementsCopyServices _mepElementsCopyServices = new();
    [ObservableProperty] private bool _isStatusVisible;
    [ObservableProperty] private string _statusMessage;

    [ObservableProperty] private int _numberOfElementsUpwards;
    [ObservableProperty] private double _distanceUp;
    [ObservableProperty] private int _numberOfElementsDown;
    [ObservableProperty] private double _distanceDown;
    [ObservableProperty] private int _numberByDirection;
    [ObservableProperty] private double _distanceByDirection;
    private XYZ _direction;
    private Reference _reference;
    private readonly JsonDataLoader _dataLoader;
    private bool _isExecutingMepElementsCopyElevation;

    public MepElementsCopyLevelsViewModel()
    {
        try
        {
            // Инициализация
            _dataLoader = new JsonDataLoader("MepElementsCopyLevels");


            // Получаем уровни из Revit
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (!levels.Any())
            {
                TaskDialog.Show("Предупреждение", "В проекте не найдены уровни");
                return;
            }

            // Загружаем сохраненные настройки
            var savedSettings = _dataLoader.LoadData<LevelDto>();
            var checkedLevelIds = savedSettings?.LevelIds ?? new List<long>();

            // Создаем модели уровней
            foreach (var level in levels)
            {
                var levelModel = new LevelModel(level)
                {
                    IsChecked = checkedLevelIds.Contains(level.Id.Value)
                };
                _levelModels.Add(levelModel);
            }

            Context.UiApplication.SelectionChanged += FindLargestNumberLevels;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка",
                $"Ошибка при инициализации: {ex.Message}");
        }
    }

    private void FindLargestNumberLevels(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        Level mostUsedLevel = FindMostUsedLevelInSelection(_doc);
        if (mostUsedLevel != null)
        {
            // Ищем соответствующую модель в коллекции вместо создания новой
            LevelModel matchingModel = LevelModels.FirstOrDefault(lm => lm.Id.Value == mostUsedLevel.Id.Value);

            if (matchingModel != null)
            {
                SelectedLevelModel = matchingModel; // Устанавливаем существующий элемент
            }
        }
    }

    /// <summary>
    /// Находит уровень, который наиболее часто используется у выделенных элементов
    /// </summary>
    /// <param name="doc">Активный документ Revit</param>
    /// <returns>Наиболее часто используемый уровень</returns>
    public Level FindMostUsedLevelInSelection(Document doc)
    {
        // Получаем текущее выделение пользователя
        UIDocument uidoc = new UIDocument(doc);
        ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

        if (selectedIds.Count == 0)
        {
            return null;
        }

        // Словарь для подсчета, сколько раз каждый уровень используется
        Dictionary<ElementId, int> levelUsageCount = new Dictionary<ElementId, int>();

        // Проходим по всем выбранным элементам
        foreach (ElementId id in selectedIds)
        {
            Element elem = doc.GetElement(id);

            // Пытаемся получить уровень элемента
            ElementId levelId = GetElementLevel(elem);

            if (levelId != null && levelId != ElementId.InvalidElementId)
            {
                // Увеличиваем счетчик использования этого уровня
                if (levelUsageCount.ContainsKey(levelId))
                {
                    levelUsageCount[levelId]++;
                }
                else
                {
                    levelUsageCount[levelId] = 1;
                }
            }
        }

        // Если не найдено ни одного уровня
        if (levelUsageCount.Count == 0)
        {
            return null;
        }

        // Находим уровень с наибольшим количеством использований
        ElementId mostUsedLevelId = levelUsageCount.OrderByDescending(x => x.Value).First().Key;

        // Возвращаем сам уровень
        return doc.GetElement(mostUsedLevelId) as Level;
    }

    /// <summary>
    /// Получает ElementId уровня, связанного с элементом
    /// </summary>
    /// <param name="elem">Элемент для проверки</param>
    /// <returns>ElementId уровня или InvalidElementId, если уровень не найден</returns>
    private ElementId GetElementLevel(Element elem)
    {
        // Проверка на null
        if (elem == null)
            return ElementId.InvalidElementId;

        // Проверяем, есть ли у элемента параметр LevelId
        Parameter levelParam = elem switch
        {
            FamilyInstance => elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM),
            MEPCurve => elem.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM),
            _ => null
        };

        if (levelParam is { HasValue: true })
        {
            return levelParam.AsElementId();
        }

        // Если не удалось определить уровень
        return ElementId.InvalidElementId;
    }

    [RelayCommand]
    private void SetBaseLevel()
    {
        if (SelectedLevelModel == null)
        {
            ShowNotification("Не выбран уровень для установки");
            return;
        }

        _actionEventHandler.Raise(_ =>
        {
            using Transaction trans = new Transaction(_doc, "Установить базовый уровень");
            trans.Start();
            try
            {
                List<ElementModel> mepElementModels = [];
                var selectedElements = _mepElementsCopyServices.GetSelectedElements(_uiDoc);
                if (selectedElements.Count == 0)
                {
                    ShowNotification("Не выбрано элементов для установки");
                    return;
                }

                mepElementModels.AddRange(selectedElements.Select(selectedElement =>
                    new ElementModel(selectedElement)));
                _mepElementsCopyServices.SetBaseLevel(mepElementModels, SelectedLevelModel);
                trans.Commit();
                ShowNotification("Базовый уровень установлен");
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", e.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand(CanExecute = nameof(CanExecuteMepElementsCopyElevation))]
    private void MepElementsCopyElevation(object window)
    {
        _isExecutingMepElementsCopyElevation = true;

        var selectLevels = LevelModels.Where(l => l.IsChecked).ToList();
        if (!selectLevels.Any())
        {
            ShowNotification("Не выбрано уровня для копирования");
            _isExecutingMepElementsCopyElevation = false;
            return;
        }

        var levelDto = new LevelDto
        {
            LevelIds = selectLevels.Select(x => x.Id.Value).ToList()
        };
        _actionEventHandler.Raise(_ =>
        {
            using Transaction trans = new Transaction(_doc, "Копирование MEP элементов по уровням");
            try
            {
                if (!GetElementsCopy(out var mepCurveModels, out var mepElementModels))
                {
                    ShowNotification("Не выбрано элементов для копирования");
                    return;
                }

                int count = mepCurveModels.Count + mepCurveModels.Count;
                trans.Start();
                if (count > 50)
                {
                    var progressBar = new ProgressWindow(selectLevels.Count);
                    progressBar.Show();
                    for (int i = 0; i < selectLevels.Count; i++)
                    {
                        progressBar.UpdateCurrentTask(
                            $"Выполняется копирование элементов на уровень \"{selectLevels[i].Name}\"");
                        if (progressBar.IsCancelling)
                        {
                            trans.RollBack();
                            return;
                        }


                        _mepElementsCopyServices.CopyMepElementsToLevel(selectLevels[i],
                            mepElementModels, mepCurveModels);
                        progressBar.UpdateProgress(i + 1);
                    }
                }
                else
                {
                    foreach (var level in selectLevels)
                    {
                        _mepElementsCopyServices.CopyMepElementsToLevel(level,
                            mepElementModels, mepCurveModels);
                    }
                }

                trans.Commit();
                ShowNotification("Копирование завершено");
            }

            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", e.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
                _dataLoader.SaveData(levelDto);
                _isExecutingMepElementsCopyElevation = false;
            }
        });
    }

    private bool CanExecuteMepElementsCopyElevation()
    {
        return !_isExecutingMepElementsCopyElevation;
    }

    private async void ShowNotification(string message)
    {
        StatusMessage = message;
        IsStatusVisible = true;

        // Ждем и скрываем статус
        await Task.Delay(3000);
        IsStatusVisible = false;
    }


    private bool GetElementsCopy(out List<MepCurveMdl> mepCurveModels, out List<ElementModel> mepElementModels)
    {
        mepCurveModels = new FilteredElementCollector(_doc)
            .OfClass(typeof(MEPCurve))
            .OfType<MEPCurve>()
            .Where(m =>
            {
                switch (m)
                {
                    case DuctInsulation:
                    case PipeInsulation:
                    case FlexDuct:
                    case FlexPipe:
                        return false;
                    default:
                        return m is not InsulationLiningBase;
                }
            })
            .Select(m => new MepCurveMdl(m)).ToList();
        mepElementModels = [];
        var selectedElements = _mepElementsCopyServices.GetSelectedElements(_uiDoc);
        if (selectedElements.Count == 0) return false;
        foreach (var selectedElement in selectedElements)
        {
            mepElementModels.Add(new ElementModel(selectedElement));
        }

        return true;
    }
    [RelayCommand]
    private void Close(object parameter)
    {
        if (parameter is Window window)
        {
            window.Close();
        }
    }
    [RelayCommand]
    private void SetDirection()
    {
        try
        {
            _reference = _uiDoc.Selection.PickObject(ObjectType.Element);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", ex.Message);
        }
    }


    [RelayCommand]
    private void CopyByDirections()
    {
        _actionEventHandler.Raise(_ =>
        {
            using Transaction trans = new Transaction(_doc, "Копирование MEP элементов в направлении");
            try
            {
                trans.Start();
                if (GetElementsCopy(out var mepCurveModels, out var mepElementModels)) return;
                if (NumberOfElementsUpwards > 0 && DistanceUp > 0)
                {
                    _mepElementsCopyServices.CopyMepElementsToDistance(DistanceUp, NumberOfElementsUpwards,
                        mepElementModels,
                        mepCurveModels);
                }

                if (NumberOfElementsDown > 0 && DistanceDown > 0)
                {
                    _mepElementsCopyServices.CopyMepElementsToDistance(DistanceDown * -1, NumberOfElementsDown,
                        mepElementModels,
                        mepCurveModels);
                }

                if (NumberByDirection > 0 && _reference != null && DistanceByDirection > 0)
                {
                    Curve curve = ((LocationCurve)_doc.GetElement(_reference).Location).Curve;
                    curve.MakeUnbound();
                    XYZ xyzPoint1 = curve.Project(_reference.GlobalPoint).XYZPoint;
                    XYZ furthestPoint = _mepElementsCopyServices.FindFurthestPoint(mepElementModels, xyzPoint1);
                    XYZ xyzPoint2 = curve.Project(furthestPoint).XYZPoint;
                    _direction = (xyzPoint1 - xyzPoint2).Normalize();
                    _mepElementsCopyServices.CopyMepElementsToDistance(DistanceByDirection, NumberByDirection,
                        mepElementModels,
                        mepCurveModels, _direction);
                }

                trans.Commit();
            }
            catch (Exception e)
            {
                trans.RollBack();
                TaskDialog.Show("Ошибка", e.Message);
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }
}