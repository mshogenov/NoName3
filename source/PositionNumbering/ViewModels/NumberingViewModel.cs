using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using NoNameApi.Services;
using PositionNumbering.Models;
using PositionNumbering.Services;

namespace PositionNumbering.ViewModels;

public sealed partial class NumberingViewModel : ObservableObject
{
    private readonly Document _doc;
    private readonly JsonDataLoader _jsonDataLoader;
    [ObservableProperty] private ObservableCollection<NumberingGroupModel> _numberingGroups = [];
    [ObservableProperty] private ObservableCollection<SystemModel> _availableSystems = [];
    [ObservableProperty] private SystemModel _selectedAvailableSystem;
    [ObservableProperty] private NumberingGroupModel _currentGroup;
    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private UIElement _currentPopupTarget;

    private readonly List<BuiltInCategory> _mepCategories =
    [
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_FlexPipeCurves,
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_PipeInsulations,
        BuiltInCategory.OST_Sprinklers,
        BuiltInCategory.OST_PlumbingEquipment,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_FlexDuctCurves,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_DuctInsulations
    ];

    [ObservableProperty] private double _initialValue = 1;
    private List<Element> _elements;
    private readonly PositionNumberingServices _positionNumberingServices;


    public NumberingViewModel()
    {
        _doc = Context.ActiveDocument;
        _jsonDataLoader = new JsonDataLoader("PositionNumbering");
        _positionNumberingServices = new PositionNumberingServices();
        // Инициализация данных
        InitializeElements();
        LoadAvailableSystems();
        LoadNumberingGroups();
    }

    private void InitializeElements()
    {
        var categoryFilter = new ElementMulticategoryFilter(_mepCategories);
        _elements = new FilteredElementCollector(_doc)
            .WherePasses(categoryFilter)
            .WhereElementIsNotElementType()
            .Where(e => e.FindParameter("ADSK_Наименование")?.AsValueString() != "!Не учитывать")
            .ToList();
    }

    private void LoadAvailableSystems()
    {
        // Получаем все используемые типы систем из элементов
        var usedSystemTypeIds = new HashSet<ElementId>();

// Из MEPCurve
        usedSystemTypeIds.UnionWith(
            _elements
                .OfType<MEPCurve>()
                .Select(mepCurve => mepCurve.MEPSystem?.GetTypeId())
                .Where(id => id != null && id != ElementId.InvalidElementId)
        );

// Из FamilyInstance
        usedSystemTypeIds.UnionWith(
            _elements
                .OfType<FamilyInstance>()
                .Where(fi => fi.MEPModel != null)
                .SelectMany(fi => fi.MEPModel.ConnectorManager?.Connectors?.Cast<Connector>() ?? [])
                .Where(c => c?.MEPSystem != null)
                .Select(c => c.MEPSystem.GetTypeId())
                .Where(id => id != ElementId.InvalidElementId)
        );

        // Загружаем только используемые MEPSystemType
        var systemModels = new FilteredElementCollector(_doc)
            .OfClass(typeof(MEPSystemType))
            .Cast<MEPSystemType>()
            .Where(systemType => usedSystemTypeIds.Contains(systemType.Id))
            .Select(systemType => new SystemModel(systemType))
            .ToList();

        foreach (var systemModel in systemModels)
        {
            AvailableSystems.Add(systemModel);
        }
    }

    private void LoadNumberingGroups()
    {
        var loadedGroups = _jsonDataLoader.LoadData<List<SettingsDto>>();

        if (loadedGroups == null || !loadedGroups.Any())
        {
            AddDefaultGroup();
        }
        else
        {
            LoadExistingGroups(loadedGroups);
        }
    }

    private void AddDefaultGroup()
    {
        var defaultGroup = new NumberingGroupModel
        {
            NumberingIsChecked = true,
            Name = $"Группа {NumberingGroups.Count + 1}",
            Systems = new ObservableCollection<SystemModel>(AvailableSystems)
        };
        AvailableSystems.Clear();
        NumberingGroups.Add(defaultGroup);
    }

    private void LoadExistingGroups(List<SettingsDto> loadedGroups)
    {
        foreach (var loadedGroup in loadedGroups)
        {
            var groupSystems = new ObservableCollection<SystemModel>();

            foreach (var system in loadedGroup.Systems)
            {
                var matchingSystem = AvailableSystems.FirstOrDefault(a => a.SystemTypeId == system.SystemTypeId);
                if (matchingSystem == null) continue;
                groupSystems.Add(system);
                AvailableSystems.Remove(matchingSystem);
            }

            var newGroup = new NumberingGroupModel
            {
                NumberingIsChecked = loadedGroup.NumberingIsChecked,
                Name = loadedGroup.Name,
                Systems = groupSystems
            };
            NumberingGroups.Add(newGroup);
        }
    }

    [RelayCommand]
    private void AddToGroup()
    {
        if (CurrentGroup != null && SelectedAvailableSystem != null)
        {
            // Добавляем систему в выбранную группу
            CurrentGroup.Systems.Add(SelectedAvailableSystem);
            // Удаляем систему из списка доступных
            AvailableSystems.Remove(SelectedAvailableSystem);
            // Сбрасываем выбранную систему
            SelectedAvailableSystem = null;
        }
    }

    [RelayCommand]
    private void RemoveGroup(NumberingGroupModel numberingGroupModel)
    {
        if (numberingGroupModel == null) return;
        // Очищаем группу у всех систем в удаляемой группе
        foreach (var system in numberingGroupModel.Systems.ToList())
        {
            AvailableSystems.Add(system);
        }

        NumberingGroups.Remove(numberingGroupModel);
    }

    [RelayCommand]
    private void RemoveFromGroup(SystemModel system)
    {
        if (system != null)
        {
            // Находим группу, из которой нужно удалить систему
            var group = NumberingGroups.FirstOrDefault(g => g.Systems.Contains(system));
            if (group != null)
            {
                group.Systems.Remove(system);

                // Добавляем систему обратно в список доступных систем, если нужно
                if (!AvailableSystems.Contains(system))
                {
                    AvailableSystems.Add(system);
                }
            }
        }
    }

    [RelayCommand]
    private void AddSystems(Button button)
    {
        if (button != null)
        {
            // Получаем контекст данных кнопки
            if (button.DataContext is NumberingGroupModel group) CurrentGroup = group;

            // Устанавливаем целевой элемент для Popup
            CurrentPopupTarget = button;

            // Открываем Popup
            IsPopupOpen = true;
        }
    }

    [RelayCommand]
    private void AddSelectedSystems()
    {
        var selectedSystems = AvailableSystems.Where(s => s.IsSelected).ToList();
        if (selectedSystems.Count > 0)
        {
            foreach (var system in selectedSystems)
            {
                CurrentGroup.Systems.Add(system);
                system.IsSelected = false; // Сброс выбора
            }

            // Обновляем список доступных систем
            UpdateAvailableSystems();
            IsPopupOpen = false;
        }
    }


    private void UpdateAvailableSystems()
    {
        var usedSystems = NumberingGroups.SelectMany(g => g.Systems).ToList();
        foreach (var system in AvailableSystems.ToList())
        {
            if (usedSystems.Contains(system))
                AvailableSystems.Remove(system);
        }
    }

    [RelayCommand]
    private void SelectAllSystems()
    {
        foreach (var availableSystem in AvailableSystems)
        {
            availableSystem.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelectionSystems()
    {
        foreach (var availableSystem in AvailableSystems)
        {
            availableSystem.IsSelected = false;
        }
    }

    [RelayCommand]
    private void ClosePopup()
    {
        IsPopupOpen = false;
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
    private void AddGroup()
    {
        var newGroup = new NumberingGroupModel
        {
            Name = $"Группа {NumberingGroups.Count + 1}",
            NumberingIsChecked = true
        };
        NumberingGroups.Add(newGroup);
        CurrentGroup = newGroup;
    }

    [RelayCommand]
    private void Numbering(object window)
    {
        var numberingGroupsChecked = NumberingGroups.Where(x => x.NumberingIsChecked).ToList();

        if (numberingGroupsChecked.Count <= 0) return;
        _positionNumberingServices.AssignPositionNumbers(numberingGroupsChecked, _elements);
        if (window is Window view)
        {
            view.Close();
        }

        List<SettingsDto> settings = new List<SettingsDto>();
        foreach (var numbering in NumberingGroups)
        {
            settings.Add(new SettingsDto()
            {
                Name = numbering.Name,
                NumberingIsChecked = numbering.NumberingIsChecked,
                Systems = numbering.Systems.ToList()
            });
        }

        _jsonDataLoader.SaveData(settings);
    }
}