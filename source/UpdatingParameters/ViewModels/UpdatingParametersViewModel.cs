using Autodesk.Revit.UI;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Autodesk.Revit.UI.Events;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Views;
using UpdatingParameters.Models;
using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.DuctInsulation;
using UpdatingParameters.Storages.Ducts;
using UpdatingParameters.Storages.FlexPipes;
using UpdatingParameters.Storages.Parameters;
using UpdatingParameters.Storages.PipeInsulationMtl;
using UpdatingParameters.Storages.Pipes;
using UpdatingParameters.Storages.Settings;
using UpdatingParameters.ViewModels.Parameters;
using UpdatingParameters.ViewModels.DuctInsulation;
using UpdatingParameters.ViewModels.Ducts;
using UpdatingParameters.ViewModels.FlexPipes;
using UpdatingParameters.ViewModels.PipeInsulation;
using UpdatingParameters.ViewModels.Pipes;
using UpdatingParameters.ViewModels.Settings;
using UpdatingParameters.Views;
using Visibility = System.Windows.Visibility;


namespace UpdatingParameters.ViewModels;

public sealed partial class UpdatingParametersViewModel : ViewModelBase
{
    private readonly Document _doc;
    private readonly ActionEventHandler _actionEventHandler = new();
    private readonly ParametersDataStorage _parametersDataStorage;
    [ObservableProperty] private ViewModelBase _selectedContent;
    [ObservableProperty] private ButtonType _selectedButton;
    [ObservableProperty] private bool _pipesWithoutButtonIsVisible;
    [ObservableProperty] private bool _allCategoriesButtonIsVisible;
    [ObservableProperty] private bool _pipesOuterDiameterButtonIsVisible;
    [ObservableProperty] private bool _pipesInternalDiameterButtonIsVisible;
    [ObservableProperty] private bool _flexPipesWithoutButtonIsVisible;
    [ObservableProperty] private bool _flexPipesConnectionsButtonIsVisible;
    [ObservableProperty] private bool _flexPipesCorrugationsButtonIsVisible;
    [ObservableProperty] private bool _pipeInsulationCylindersButtonIsVisible;
    [ObservableProperty] private bool _pipeInsulationTubesButtonIsVisible;
    [ObservableProperty] private bool _pipeInsulationColouredTubesButtonIsVisible;
    [ObservableProperty] private bool _ductWithoutButtonIsVisible;
    [ObservableProperty] private bool _ductRoundButtonIsVisible;
    [ObservableProperty] private bool _ductPlasticButtonIsVisible;
    [ObservableProperty] private bool _ductRectangularButtonIsVisible;
    [ObservableProperty] private bool _flexibleDuctsRoundButtonIsVisible;
    [ObservableProperty] private bool _ductInsulationFireproofingButtonIsVisible;
    [ObservableProperty] private bool _ductInsulationThermalInsulationButtonIsVisible;
    [ObservableProperty] private bool _ductConnectionPartsButtonIsVisible;
    private readonly PipesWithoutDataStorage _pipesWithoutDataStorage;
    private readonly PipesOuterDiameterDataStorage _pipesOuterDiameterDataStorage;
    private readonly PipesInternalDiameterDataStorage _pipesInternalDiameterDataStorage;
    private readonly FlexPipeWithoutDataStorage _flexPipeWithoutDataStorage;
    private readonly FlexPipesCorrugationsDataStorage _flexPipesCorrugationsDataStorage;
    private readonly FlexPipesConnectionsDataStorage _flexPipesConnectionsDataStorage;
    private readonly PipeInsulationCylindersDataStorage _pipeInsulationCylindersDataStorage;
    private readonly PipeInsulationTubesDataStorage _pipeInsulationTubesDataStorage;
    private readonly PipeInsulationColouredTubesDataStorage _insulationColouredTubesDataStorage;
    private readonly DuctInsulationFireproofingDataStorage _ductInsulationFireproofingDataStorage;
    private readonly DuctInsulationThermalDataStorage _ductInsulationThermalDataStorage;
    private readonly DuctConnectionPartsDataStorage _ductConnectionPartsDataStorage;
    private readonly DuctPlasticDataStorage _ductPlasticDataStorage;
    private readonly DuctRectangularDataStorage _ductRectangularDataStorage;
    private readonly DuctRoundDataStorage _ductRoundDataStorage;
    private readonly DuctWithoutDataStorage _ductWithoutDataStorage;
    private readonly FlexibleDuctsRoundDataStorage _flexibleDuctsRoundDataStorage;
    private readonly SettingsDataStorage _settingsDataStorage;
    private readonly DuctParametersDataStorage _ductParametersDataStorage;
    private PipesWithoutViewModel _pipesWithoutViewModel;
    private PipesOuterDiameterViewModel _pipesOuterDiameterViewModel;
    private PipesInternalDiameterViewModel _pipesInternalDiameterViewModel;
    private FlexPipesWithoutViewModel _flexPipesWithoutViewModel;
    private FlexPipesCorrugationsViewModel _flexPipesCorrugationsViewModel;
    private FlexPipesConnectionsViewModel _flexPipesConnectionsViewModel;
    private PipeInsulationTubesViewModel _pipeInsulationTubesViewModel;
    private PipeInsulationCylindersViewModel _pipeInsulationCylindersViewModel;
    private SettingsViewModel _settingsViewModel;
    private PipeInsulationColouredTubesViewModel _pipeInsulationColouredTubesViewModel;
    private DuctWithoutViewModel _ductWithoutViewModel;
    private DuctRoundViewModel _ductRoundViewModel;
    private DuctPlasticViewModel _ductPlasticViewModel;
    private DuctRectangularViewModel _ductRectangularViewModel;
    private DuctConnectionPartsViewModel _ductConnectionPartsViewModel;
    private DuctInsulationThermalInsulationViewModel _ductInsulationThermalViewModel;
    private FlexibleDuctsRoundViewModel _flexibleDuctsRoundViewModel;
    private DuctInsulationFireproofingViewModel _ductInsulationFireproofingViewModel;
    private ParametersViewModel _parametersViewModel;
    private MainViewModel _mainViewModel;

    private readonly List<ElementId> _mepCategories =
    [
        new(BuiltInCategory.OST_PipeCurves),
        new(BuiltInCategory.OST_PlumbingFixtures),
        new(BuiltInCategory.OST_FlexPipeCurves),
        new(BuiltInCategory.OST_MechanicalEquipment),
        new(BuiltInCategory.OST_PipeAccessory),
        new(BuiltInCategory.OST_PipeFitting),
        new(BuiltInCategory.OST_PipeInsulations),
        new(BuiltInCategory.OST_Sprinklers),
        new(BuiltInCategory.OST_PlumbingEquipment),
        new(BuiltInCategory.OST_DuctCurves),
        new(BuiltInCategory.OST_DuctFitting),
        new(BuiltInCategory.OST_FlexDuctCurves),
        new(BuiltInCategory.OST_DuctAccessory),
        new(BuiltInCategory.OST_DuctTerminal),
        new(BuiltInCategory.OST_DuctInsulations),
        new(BuiltInCategory.OST_DuctLinings)
    ];

    private readonly int _allElementsCount;

    private ProgressWindow _progressWindow;
    private readonly DataStorageFactory _storageFactory;


    public UpdatingParametersViewModel()
    {
        _doc = Context.ActiveDocument;
        var categoryFilter = new ElementMulticategoryFilter(_mepCategories);
        var collector = new FilteredElementCollector(_doc).WherePasses(categoryFilter)
            .WhereElementIsNotElementType().ToElements();
        _allElementsCount = collector.Count;
        _storageFactory = new DataStorageFactory();
        _storageFactory.InitializeAllStorages();
        _pipesWithoutDataStorage =_storageFactory.GetStorage<PipesWithoutDataStorage>();
        _pipesOuterDiameterDataStorage = _storageFactory.GetStorage<PipesOuterDiameterDataStorage>();
        _pipesInternalDiameterDataStorage = _storageFactory.GetStorage<PipesInternalDiameterDataStorage>();
        _flexPipeWithoutDataStorage = _storageFactory.GetStorage<FlexPipeWithoutDataStorage>();
        _flexPipesCorrugationsDataStorage = _storageFactory.GetStorage<FlexPipesCorrugationsDataStorage>();
        _flexPipesConnectionsDataStorage = _storageFactory.GetStorage<FlexPipesConnectionsDataStorage>();
        _pipeInsulationCylindersDataStorage =
            _storageFactory.GetStorage<PipeInsulationCylindersDataStorage>();
        _pipeInsulationTubesDataStorage = _storageFactory.GetStorage<PipeInsulationTubesDataStorage>();
        _insulationColouredTubesDataStorage =
            _storageFactory.GetStorage<PipeInsulationColouredTubesDataStorage>();
        _ductInsulationFireproofingDataStorage =
            _storageFactory.GetStorage<DuctInsulationFireproofingDataStorage>();
        _ductInsulationThermalDataStorage = _storageFactory.GetStorage<DuctInsulationThermalDataStorage>();
        _ductConnectionPartsDataStorage = _storageFactory.GetStorage<DuctConnectionPartsDataStorage>();
        _ductPlasticDataStorage = _storageFactory.GetStorage<DuctPlasticDataStorage>();
        _ductRectangularDataStorage = _storageFactory.GetStorage<DuctRectangularDataStorage>();
        _ductRoundDataStorage = _storageFactory.GetStorage<DuctRoundDataStorage>();
        _ductWithoutDataStorage = _storageFactory.GetStorage<DuctWithoutDataStorage>();
        _flexibleDuctsRoundDataStorage = _storageFactory.GetStorage<FlexibleDuctsRoundDataStorage>();
        _settingsDataStorage = _storageFactory.GetStorage<SettingsDataStorage>();
        _ductParametersDataStorage = _storageFactory.GetStorage<DuctParametersDataStorage>();
        _parametersDataStorage = _storageFactory.GetStorage<ParametersDataStorage>();
        AllocationDataStorages(collector);
        InitializeViewModels();
    }

    private void AllocationDataStorages(IList<Element> collector)
    {
        foreach (var element in collector)
        {
            _parametersDataStorage.AddElement(element);
            switch (element.Category.BuiltInCategory)
            {
                case BuiltInCategory.OST_PipeCurves:

                    switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                    {
                        case "Днар х Стенка":
                            _pipesOuterDiameterDataStorage.AddElement(element);
                            break;
                        case "Ду х Стенка":
                            _pipesInternalDiameterDataStorage.AddElement(element);
                            break;
                        default:
                            _pipesWithoutDataStorage.AddElement(element);
                            break;
                    }

                    break;
                case BuiltInCategory.OST_FlexPipeCurves:
                    switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                    {
                        case "Подводки":
                            _flexPipesConnectionsDataStorage.AddElement(element);
                            break;
                        case "Гофры":
                            _flexPipesCorrugationsDataStorage.AddElement(element);
                            break;
                        default:
                            _flexPipeWithoutDataStorage.AddElement(element);
                            break;
                    }

                    break;
                case BuiltInCategory.OST_PipeInsulations:
                    switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                    {
                        case "Трубки":
                            _pipeInsulationTubesDataStorage.AddElement(element);
                            break;
                        case "Цилиндры":
                            _pipeInsulationCylindersDataStorage.AddElement(element);
                            break;
                        case "Трубки цветные":
                            _insulationColouredTubesDataStorage.AddElement(element); break;
                    }

                    break;
                case BuiltInCategory.OST_DuctCurves:
                    switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                    {
                        case "Пластик":
                            _ductPlasticDataStorage.AddElement(element); break;
                        case "Прямоугольные":
                            _ductRectangularDataStorage.AddElement(element); break;
                        case "Круглые":
                            _ductRoundDataStorage.AddElement(element); break;
                        default:
                            _ductWithoutDataStorage.AddElement(element); break;
                    }

                    break;
                case BuiltInCategory.OST_DuctInsulations:
                    switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                    {
                        case "Огнезащита":
                            _ductInsulationFireproofingDataStorage.AddElement(element); break;
                        case "Теплоизоляция":
                            _ductInsulationThermalDataStorage.AddElement(element); break;
                    }

                    break;
                case BuiltInCategory.OST_FlexDuctCurves:
                    switch (element.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString())
                    {
                        case "Круглые":
                            _flexibleDuctsRoundDataStorage.AddElement(element); break;
                    }

                    break;
                case BuiltInCategory.OST_DuctFitting:
                    _ductConnectionPartsDataStorage.AddElement(element); break;
            }
        }
    }

    private void InitializeViewModels()
    {
        _mainViewModel = new MainViewModel();
        _parametersViewModel = new ParametersViewModel(_storageFactory);

        if (_pipesWithoutDataStorage.GetElements().Count != 0)
        {
            _pipesWithoutViewModel = new PipesWithoutViewModel(_pipesWithoutDataStorage);
            PipesWithoutButtonIsVisible = true;
        }

        if (_pipesOuterDiameterDataStorage.GetElements().Count != 0)
        {
            _pipesOuterDiameterViewModel = new PipesOuterDiameterViewModel(_pipesOuterDiameterDataStorage);
            PipesOuterDiameterButtonIsVisible = true;
        }

        if (_pipesInternalDiameterDataStorage.GetElements().Count != 0)
        {
            _pipesInternalDiameterViewModel = new PipesInternalDiameterViewModel(_pipesInternalDiameterDataStorage);
            PipesInternalDiameterButtonIsVisible = true;
        }

        if (_flexPipeWithoutDataStorage.GetElements().Count != 0)
        {
            _flexPipesWithoutViewModel = new FlexPipesWithoutViewModel(_flexPipeWithoutDataStorage);
            FlexPipesWithoutButtonIsVisible = true;
        }

        if (_flexPipesCorrugationsDataStorage.GetElements().Count != 0)
        {
            _flexPipesCorrugationsViewModel = new FlexPipesCorrugationsViewModel(_flexPipesCorrugationsDataStorage);
            FlexPipesCorrugationsButtonIsVisible = true;
        }

        if (_flexPipesConnectionsDataStorage.GetElements().Count != 0)
        {
            _flexPipesConnectionsViewModel = new FlexPipesConnectionsViewModel(_flexPipesConnectionsDataStorage);
            FlexPipesConnectionsButtonIsVisible = true;
        }

        if (_pipeInsulationTubesDataStorage.GetElements().Count != 0)
        {
            _pipeInsulationTubesViewModel = new PipeInsulationTubesViewModel(_pipeInsulationTubesDataStorage);
            PipeInsulationTubesButtonIsVisible = true;
        }

        if (_pipeInsulationCylindersDataStorage.GetElements().Count != 0)
        {
            _pipeInsulationCylindersViewModel =
                new PipeInsulationCylindersViewModel(_pipeInsulationCylindersDataStorage);
            PipeInsulationCylindersButtonIsVisible = true;
        }

        if (_insulationColouredTubesDataStorage.GetElements().Count != 0)
        {
            _pipeInsulationColouredTubesViewModel =
                new PipeInsulationColouredTubesViewModel(_insulationColouredTubesDataStorage);
            PipeInsulationColouredTubesButtonIsVisible = true;
        }

        if (_ductWithoutDataStorage.GetElements().Count != 0)
        {
            _ductWithoutViewModel = new DuctWithoutViewModel(_ductWithoutDataStorage,_storageFactory);
            DuctWithoutButtonIsVisible = true;
        }

        if (_ductRoundDataStorage.GetElements().Count != 0)
        {
            _ductRoundViewModel = new DuctRoundViewModel(_ductRoundDataStorage,_storageFactory);
            DuctRoundButtonIsVisible = true;
        }

        if (_ductPlasticDataStorage.GetElements().Count != 0)
        {
            _ductPlasticViewModel = new DuctPlasticViewModel(_ductPlasticDataStorage,_storageFactory);
            DuctPlasticButtonIsVisible = true;
        }

        if (_ductRectangularDataStorage.GetElements().Count != 0)
        {
            _ductRectangularViewModel =
                new DuctRectangularViewModel(_ductRectangularDataStorage,_storageFactory);
            DuctRectangularButtonIsVisible = true;
        }

        if (_ductConnectionPartsDataStorage.GetElements().Count != 0)
        {
            _ductConnectionPartsViewModel =
                new DuctConnectionPartsViewModel(_ductConnectionPartsDataStorage,_storageFactory);
            DuctConnectionPartsButtonIsVisible = true;
        }

        if (_ductInsulationThermalDataStorage.GetElements().Count != 0)
        {
            _ductInsulationThermalViewModel =
                new DuctInsulationThermalInsulationViewModel(_ductInsulationThermalDataStorage);
            DuctInsulationThermalInsulationButtonIsVisible = true;
        }

        if (_flexibleDuctsRoundDataStorage.GetElements().Count != 0)
        {
            _flexibleDuctsRoundViewModel = new FlexibleDuctsRoundViewModel(_flexibleDuctsRoundDataStorage,_storageFactory);
            FlexibleDuctsRoundButtonIsVisible = true;
        }

        if (_ductInsulationFireproofingDataStorage.GetElements().Count != 0)
        {
            _ductInsulationFireproofingViewModel =
                new DuctInsulationFireproofingViewModel(_ductInsulationFireproofingDataStorage);
            DuctInsulationFireproofingButtonIsVisible = true;
        }

        _settingsViewModel = new SettingsViewModel(_settingsDataStorage);
    }

    [RelayCommand]
    private void Main()
    {
        SelectedContent = _mainViewModel;
        SelectedButton = ButtonType.Main;
    }

    [RelayCommand]
    private void Parameters()
    {
        SelectedContent = _parametersViewModel;
        SelectedButton = ButtonType.AllCategories;
    }

    [RelayCommand]
    private void PipesWithout()
    {
        SelectedContent = _pipesWithoutViewModel;
        SelectedButton = ButtonType.PipesWithout;
    }

    [RelayCommand]
    private void PipesOuterDiameter()
    {
        SelectedContent = _pipesOuterDiameterViewModel;
        SelectedButton = ButtonType.PipesOuterDiameter;
    }

    [RelayCommand]
    private void PipesInternalDiameter()
    {
        SelectedContent = _pipesInternalDiameterViewModel;
        SelectedButton = ButtonType.PipesInternalDiameter;
    }

    [RelayCommand]
    private void FlexPipesWithout()
    {
        SelectedContent = _flexPipesWithoutViewModel;
        SelectedButton = ButtonType.FlexiblePipesWithoutType;
    }

    [RelayCommand]
    private void FlexPipesConnections()
    {
        SelectedContent = _flexPipesConnectionsViewModel;
        SelectedButton = ButtonType.FlexiblePipesConnections;
    }

    [RelayCommand]
    private void FlexPipesCorrugations()
    {
        SelectedContent = _flexPipesCorrugationsViewModel;
        SelectedButton = ButtonType.FlexiblePipesCorrugations;
    }

    [RelayCommand]
    private void PipeInsulationTubes()
    {
        SelectedContent = _pipeInsulationTubesViewModel;
        SelectedButton = ButtonType.PipeInsulationTubes;
    }

    [RelayCommand]
    private void PipeInsulationCylinders()
    {
        SelectedContent = _pipeInsulationCylindersViewModel;
        SelectedButton = ButtonType.PipeInsulationCylinders;
    }

    [RelayCommand]
    private void PipeInsulationColouredTubes()
    {
        SelectedContent = _pipeInsulationColouredTubesViewModel;
        SelectedButton = ButtonType.PipeInsulationColouredTubes;
    }

    [RelayCommand]
    private void DuctWithout()
    {
        SelectedContent = _ductWithoutViewModel;
        SelectedButton = ButtonType.DuctWithout;
    }

    [RelayCommand]
    private void DuctRound()
    {
        SelectedContent = _ductRoundViewModel;
        SelectedButton = ButtonType.DuctRound;
    }

    [RelayCommand]
    private void DuctPlastic()
    {
        SelectedContent = _ductPlasticViewModel;
        SelectedButton = ButtonType.DuctPlastic;
    }

    [RelayCommand]
    private void DuctRectangular()
    {
        SelectedContent = _ductRectangularViewModel;
        SelectedButton = ButtonType.DuctRectangular;
    }

    [RelayCommand]
    private void FlexibleDuctsRound()
    {
        SelectedContent = _flexibleDuctsRoundViewModel;
        SelectedButton = ButtonType.FlexibleDuctsRound;
    }

    [RelayCommand]
    private void DuctInsulationFireproofing()
    {
        SelectedContent = _ductInsulationFireproofingViewModel;
        SelectedButton = ButtonType.DuctInsulationFireproofing;
    }

    [RelayCommand]
    private void DuctInsulationThermalInsulation()
    {
        SelectedContent = _ductInsulationThermalViewModel;
        SelectedButton = ButtonType.DuctInsulationThermalInsulation;
    }

    [RelayCommand]
    private void DuctConnectionParts()
    {
        SelectedContent = _ductConnectionPartsViewModel;
        SelectedButton = ButtonType.DuctConnectionParts;
    }

    [RelayCommand]
    private void Settings()
    {
        SelectedContent = _settingsViewModel;
        SelectedButton = ButtonType.Settings;
    }


    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var dataStorages = _storageFactory.GetAllStorages();
            foreach (var dataStorage in dataStorages)
            {
                dataStorage.Save();
            }

            MessageBox.Show("Настройки сохранены", "Информация");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка");
        }
    }

    [RelayCommand]
    private void CloseWindow(object obj)
    {
        if (obj is Window window)
        {
            window.Close();
        }
    }


    [RelayCommand]
    private void UpdateAllTypes(object window)
    {
        var modalWindow = window as Window;
        var updateActions = UpdateActions();
        var updateResults = new List<UpdateResult>();
        try
        {
            if (_allElementsCount > 1000)
            {
                InitializeProgressWorkflow(modalWindow, updateActions);
                ExecuteLongUpdateWithProgress(modalWindow, updateActions, updateResults);
            }
            else
            {
                ExecuteDirectUpdate(updateActions, updateResults);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, modalWindow);
        }
    }

// Вынесенная общая логика
    private void InitializeProgressWorkflow(Window modalWindow,
        List<(string TypeName, Func<int> UpdateAction)> updateActions)
    {
        SetWindowVisibility(modalWindow, Visibility.Hidden);
        modalWindow.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
        var totalCategories = updateActions.Count;
        _progressWindow = new ProgressWindow(totalCategories);
        _progressWindow.Show();
    }

// Основной рабочий процесс с прогресс-баром
    private void ExecuteLongUpdateWithProgress(Window modalWindow,
        List<(string TypeName, Func<int> UpdateAction)> updateActions, List<UpdateResult> results)
    {
        _actionEventHandler.Raise(app =>
        {
            using var tr = new Transaction(app.ActiveUIDocument.Document, "Обновление параметров");
            try
            {
                tr.Start();
                UpdateParameters();
                int currentIndex = 0;
                foreach (var (typeName, updateAction) in updateActions)
                {
                    _progressWindow.Dispatcher.Invoke(() =>
                    {
                        _progressWindow.UpdateCurrentTask($"Обработка: {typeName}");
                    });
                    if (CheckForCancellation(modalWindow))
                    {
                        tr.RollBack();
                        return;
                    }

                    ExecuteUpdateAction(typeName, updateAction, results);
                    currentIndex++;
                    var index = currentIndex;
                    _progressWindow.Dispatcher.Invoke(() => { _progressWindow.UpdateProgress(index); });
                    _progressWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
                }

                tr.Commit();
                FinalizeProgressWorkflow(modalWindow, results);
            }
            catch
            {
                tr.RollBack();
                throw;
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

// Прямое выполнение без прогресс-бара
    private void ExecuteDirectUpdate(List<(string TypeName, Func<int> UpdateAction)> updateActions,
        List<UpdateResult> results)
    {
        using (var tr = new Transaction(_doc, "Обновление параметров"))
        {
            tr.Start();
            UpdateParameters();
            foreach (var (typeName, updateAction) in updateActions)
            {
                ExecuteUpdateAction(typeName, updateAction, results);
            }

            tr.Commit();
        }

        ProcessUpdateResults(results); // Обрабатываем результаты
    }

// Общие вспомогательные методы
    private bool CheckForCancellation(Window modalWindow)
    {
        if (!_progressWindow.IsCancelling)
            return false;

        _progressWindow.Close();
        TaskDialog.Show("Отмена", "Операция была отменена пользователем.");
        SetWindowVisibility(modalWindow, Visibility.Visible);
        return true;
    }


    private void ExecuteUpdateAction(string typeName, Func<int> updateAction, List<UpdateResult> results)
    {
        var updatedCount = updateAction();
        results.Add(new UpdateResult
        {
            TypeName = typeName,
            UpdatedCount = updatedCount
        });
    }

    private void FinalizeProgressWorkflow(Window modalWindow, List<UpdateResult> results)
    {
        _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close());
        ProcessUpdateResults(results); // Передаем результаты
        SetWindowVisibility(modalWindow, Visibility.Visible);
    }

    private void SetWindowVisibility(Window window, Visibility visibility)
    {
        window?.Dispatcher.Invoke(() =>
        {
            window.Visibility = visibility;
            if (visibility == Visibility.Visible)
                window.Activate();
        });
    }

    private void HandleError(Exception ex, Window modalWindow)
    {
        TaskDialog.Show("Ошибка", $"{ex.Message}\n\n{ex.StackTrace}");
        SetWindowVisibility(modalWindow, Visibility.Visible);
        // Добавьте логирование ошибки при необходимости
        // Logger.LogError(ex);
    }


    private void ProcessUpdateResults(List<UpdateResult> updateResults)
    {
        // Фильтруем результаты, исключая типы с нулевым количеством обновлений
        var filteredResults = updateResults.Where(result => result.UpdatedCount > 0).ToList();

        // Выводим результаты
        if (filteredResults.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Обновлено элементов по типам:");
            foreach (var result in filteredResults)
            {
                sb.AppendLine($"{result.TypeName}: {result.UpdatedCount}");
            }

            // Отображение сообщения в окне
            MessageBox.Show(sb.ToString(), "Результаты обновления");
        }
        else
        {
            // Сообщение, если нет обновленных элементов
            MessageBox.Show("Обновлений не было выполнено", "Результаты обновления");
        }
    }

    private List<(string TypeName, Func<int> UpdateAction)> UpdateActions()
    {
        var updateActions = new List<(string TypeName, Func<int> UpdateAction)>
        {
            ("Трубы: Без типа", () => UpdateCategoriesTypes(_pipesWithoutDataStorage)),
            ("Трубы: Ду х Стенка", () => UpdateCategoriesTypes(_pipesOuterDiameterDataStorage)),
            ("Трубы: Днар х Стенка", () => UpdateCategoriesTypes(_pipesInternalDiameterDataStorage)),
            ("Гибкие трубы: Без типа", () => UpdateCategoriesTypes(_flexPipeWithoutDataStorage)),
            ("Гибкие трубы: Гофры", () => UpdateCategoriesTypes(_flexPipesCorrugationsDataStorage)),
            ("Гибкие трубы: Подводки", () => UpdateCategoriesTypes(_flexPipesConnectionsDataStorage)),
            ("Материал изоляции трубы: Цилиндры", () => UpdateCategoriesTypes(_pipeInsulationCylindersDataStorage)),
            ("Материал изоляции трубы: Трубки", () => UpdateCategoriesTypes(_pipeInsulationTubesDataStorage)),
            ("Материал изоляции трубы: Трубки цветные",
                () => UpdateCategoriesTypes(_insulationColouredTubesDataStorage)),
            ("Воздуховоды: Без типа", () => UpdateCategoriesTypes(_ductWithoutDataStorage)),
            ("Воздуховоды: Круглые", () => UpdateCategoriesTypes(_ductRoundDataStorage)),
            ("Воздуховоды: Пластик", () => UpdateCategoriesTypes(_ductPlasticDataStorage)),
            ("Воздуховоды: Прямоугольные", () => UpdateCategoriesTypes(_ductRectangularDataStorage)),
            ("Гибкие воздуховоды: Круглые", () => UpdateCategoriesTypes(_flexibleDuctsRoundDataStorage)),
            ("Материал изоляции воздуховодов: Огнезащита",
                () => UpdateCategoriesTypes(_ductInsulationFireproofingDataStorage)),
            ("Материал изоляции воздуховодов: Теплоизоляция",
                () => UpdateCategoriesTypes(_ductInsulationThermalDataStorage)),
            ("Соединительные детали воздуховодов", () => UpdateCategoriesTypes(_ductConnectionPartsDataStorage))
        };
        return updateActions;
    }

    private void UpdateParameters()
    {
        ParametersDataStorage dataStorage = _storageFactory.GetStorage<ParametersDataStorage>();

        // Получаем элементы и отфильтровываем недействительные
        var elements = dataStorage.GetElements()
            .Where(x => x != null && x.IsValidObject)
            .ToList();

        try
        {
            var nestedFamilies = elements.OfType<FamilyInstance>()
                .Where(fi => fi != null && fi.IsValidObject)
                .SelectMany(fi =>
                {
                    try
                    {
                        return fi.GetSubComponentIds().Where(subId => subId != null);
                    }
                    catch (Exception)
                    {
                        // Если возникла ошибка при получении подкомпонентов, возвращаем пустой список
                        return Enumerable.Empty<ElementId>();
                    }
                })
                .ToList();

            // Отделение от всех семейств вложенных семейств
            var elementsIdNotNestedFamilies = elements
                .Where(x => x != null && x.IsValidObject)
                .Select(x => x.Id)
                .Except(nestedFamilies)
                .Select(x => x.ToElement(Context.ActiveDocument))
                .Where(x => x != null)
                .ToList();

            // Дальнейший код остается без изменений
            if (dataStorage.SystemAbbreviationIsChecked)
            {
                UpdaterParametersService.UpdateParamSystemAbbreviation(_doc, elementsIdNotNestedFamilies);
            }

            if (dataStorage.SystemNameIsChecked)
            {
                UpdaterParametersService.UpdateParamSystemName(_doc, elementsIdNotNestedFamilies);
            }

            if (dataStorage.HermeticClassIsChecked)
            {
                UpdaterParametersService.UpdateParamHermeticСlass(_doc, elements);
            }

            if (dataStorage.WallThicknessIsChecked)
            {
                UpdaterParametersService.UpdateParamWallThickness(_doc, elements,
                    _ductParametersDataStorage.DuctParameters);
            }
        }
        catch (Exception ex)
        {
            // Здесь вы можете добавить логирование или показать пользователю сообщение об ошибке
            // TaskDialog.Show("Ошибка", "Произошла ошибка при обновлении параметров: " + ex.Message);
        }
    }

    private int UpdateCategoriesTypes(DataStorageFormulas dataStorage)
    {
        var elements = dataStorage.GetElements();
        if (elements.Count == 0) return 0;
        int updatedCount = 0;

        foreach (var element in elements)
        {
            UpdaterParametersService.UpdateParameters(element, dataStorage);

            updatedCount++;
        }

        return updatedCount;
    }
}