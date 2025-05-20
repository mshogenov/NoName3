using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using NoNameApi.Utils;
using ViewOfPipeSystems.Model;
using ViewOfPipeSystems.Services;

namespace ViewOfPipeSystems.ViewModels;

public partial class ViewOfPipeSystemsVM : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private List<MEPSystemTypeModel> _mepSystemTypeModels = [];

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CreateViewsCommand))]
    private bool _isVisibilityMissingParameters;

    [ObservableProperty] private string _paramADSK_Система_Имя = "ADSK_Система_Имя";
    [ObservableProperty] private bool _isVisibilityMissingParametersADSK_Система_Имя;
    [ObservableProperty] private bool _isStatusVisible;
    private readonly ICollection<BuiltInCategory> _mepSystemCategories =
    [
        BuiltInCategory.OST_PipingSystem,
        BuiltInCategory.OST_DuctSystem
    ];

    [ObservableProperty] private string _statusMessage;
    private readonly ViewOfPipeSystemsServices _viewOfPipeSystemsServices;

    public ViewOfPipeSystemsVM()
    {
        IsVisibilityMissingParametersADSK_Система_Имя =
            !Helpers.CheckParameterExists(_doc, _paramADSK_Система_Имя);
        if (IsVisibilityMissingParametersADSK_Система_Имя)
        {
            IsVisibilityMissingParameters = true;
        }
        

        _viewOfPipeSystemsServices = new ViewOfPipeSystemsServices();
        // Получаем все используемые системы в проекте
        var usedSystems = new HashSet<ElementId>();
        var categoryFilter = new ElementMulticategoryFilter(_mepSystemCategories);
        // Получаем все MEP элементы в проекте, которые принадлежат системам
        var mepElements = new FilteredElementCollector(_doc)
            .WherePasses(categoryFilter)
            .WhereElementIsNotElementType()
            .Cast<MEPSystem>()
            .ToList();

        // Добавляем ID системного типа в наш HashSet
        foreach (var mepSystem in mepElements)
        {
            usedSystems.Add(mepSystem.GetTypeId());
        }

        // Получаем все типы MEP систем
        var allSystemTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(MEPSystemType))
            .WhereElementIsElementType()
            .Cast<MEPSystemType>();

        // Добавляем в модель только те системные типы, которые используются
        foreach (var mepSystemType in allSystemTypes)
        {
            if (usedSystems.Contains(mepSystemType.Id))
            {
                MepSystemTypeModels.Add(new MEPSystemTypeModel(mepSystemType));
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateViews))]
    private void CreateViews()
    {
        var selectedMepSystem = MepSystemTypeModels
            .Select(x => x.MEPSystemModels)
            .SelectMany(x => x.Where(mepSystemModel => mepSystemModel.IsChecked)).ToList();
        var existingViews = new FilteredElementCollector(_doc)
            .OfClass(typeof(View))
            .Cast<View>()
            .GroupBy(v => v.Name, StringComparer.OrdinalIgnoreCase) // Группируем по имени
            .Select(g => g.First()) // Берем первый из попавших в группу
            .ToDictionary(v => v.Name, v => v.Id, StringComparer.OrdinalIgnoreCase);
        var existingFilters = new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>()
            .ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);
        if (selectedMepSystem.Any())
        {
            using Transaction tr = new(_doc, "Виды систем");
            tr.Start();
            try
            {
                
                _viewOfPipeSystemsServices.ProcessMepSystems(selectedMepSystem,existingViews,existingFilters);
                tr.Commit();
                ShowNotification("Виды созданы");
            }
            catch (Exception e)
            {
                tr.RollBack();
                TaskDialog.Show("Ошибка", e.Message);
            }
           
        }
    }

    private bool CanCreateViews()
    {
        return !IsVisibilityMissingParameters;
    }
    private async void ShowNotification(string message)
    {
        StatusMessage = message;
        IsStatusVisible = true;

        // Ждем и скрываем статус
        await Task.Delay(3000);
        IsStatusVisible = false;
    }

    
}