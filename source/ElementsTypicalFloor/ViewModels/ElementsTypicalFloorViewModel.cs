using Autodesk.Revit.UI;
using ElementsTypicalFloor.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using System.Collections.ObjectModel;
using System.Windows;
using ElementsTypicalFloor.Models;
using NoNameApi.Services;
using NoNameApi.Utils;


namespace ElementsTypicalFloor.ViewModels;

public sealed partial class ElementsTypicalFloorViewModel : ObservableObject
{
    private ActionEventHandler ActionEventHandler { get; set; } = new();
    [ObservableProperty] private string _paramMshNumberWithTypicalFloors = "msh_Количество с типовыми этажами";
    [ObservableProperty] private string _paramMshTypeFloorElement = "msh_Элемент типового этажа";
    [ObservableProperty] private bool _isVisibilityMissingParameters;

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
        BuiltInCategory.OST_DuctInsulations,
        BuiltInCategory.OST_DuctLinings
    ];

    [ObservableProperty] private ElementId _groundFloorId;
    [ObservableProperty] private ObservableCollection<Level> _levels;
    [ObservableProperty] private ElementId _finalFloorId;
    [ObservableProperty] private string _groundMark;
    [ObservableProperty] private string _finalMark;
    [ObservableProperty] private int _typicalFloorsCount = 1;
    private readonly UIDocument _uidoc;
    private readonly Document _doc;
    private readonly ElementsTypicalFloorService _elementsTypicalFloorService;
    [ObservableProperty] private bool _isVisibilityMissingParametersMshNumberWithTypicalFloors;
    [ObservableProperty] private bool _isVisibilityMissingParametersMshElementOfTypicalStorey;
    private readonly JsonDataLoader _jsonDataLoader;


    public ElementsTypicalFloorViewModel()
    {
        _uidoc = Context.ActiveUiDocument;
        _doc = Context.ActiveDocument;
        _jsonDataLoader = new JsonDataLoader("ElementsTypicalFloor");
        var loadTypicalFloorsCount = _jsonDataLoader.LoadData<ElementsTypicalFloorDto>();
        if (loadTypicalFloorsCount != null)
        {
            _typicalFloorsCount = loadTypicalFloorsCount.TypicalFloorsCount;
        }
        _elementsTypicalFloorService = new ElementsTypicalFloorService();
        _isVisibilityMissingParametersMshNumberWithTypicalFloors =
            !Helpers.CheckParameterExists(_doc, _paramMshNumberWithTypicalFloors);
        _isVisibilityMissingParametersMshElementOfTypicalStorey =
            !Helpers.CheckParameterExists(_doc, _paramMshTypeFloorElement);
        if (IsVisibilityMissingParametersMshNumberWithTypicalFloors ||
            IsVisibilityMissingParametersMshElementOfTypicalStorey)
        {
            _isVisibilityMissingParameters = true;
        }
    }

    [RelayCommand]
    private void UpdateElements()
    {
        ActionEventHandler.Raise(_ =>
        {
            try
            {
                _elementsTypicalFloorService.UpdateElementParametersForTypicalFloors(_mepCategories,
                    ParamMshNumberWithTypicalFloors, ParamMshTypeFloorElement, TypicalFloorsCount);
                _jsonDataLoader.SaveData(new ElementsTypicalFloorDto { TypicalFloorsCount = TypicalFloorsCount });
            }
            catch (Exception e)
            {
                TaskDialog.Show("Ошибка", e.Message);
            }
            finally
            {
                ActionEventHandler.Cancel();
            }
        });
    }

    [RelayCommand]
    private void SelectedElements()
    {
        var categoryFilter = new ElementMulticategoryFilter(_mepCategories);
        var collector = new FilteredElementCollector(_doc)
            .WherePasses(categoryFilter)
            .WhereElementIsNotElementType()
            .Where(el => el.FindParameter(ParamMshTypeFloorElement)?.AsBool() == true)
            .Select(e => e.Id).ToList();
        _elementsTypicalFloorService.SelectedElements(_uidoc, collector);
    }

    [RelayCommand]
    private void LoadSharedParamMshNumberWithTypicalFloors()
    {
        ActionEventHandler.Raise(_ =>
        {
            using Transaction tr = new Transaction(_doc, "Добавить общий параметр");
            tr.Start();
            try
            {
                var isCreate = Helpers.CreateSharedParameter(_doc, ParamMshNumberWithTypicalFloors,
                    SpecTypeId.Number, GroupTypeId.Data, true, _mepCategories);
                if (isCreate)
                {
                    IsVisibilityMissingParametersMshNumberWithTypicalFloors = false;
                }

                if (!IsVisibilityMissingParametersMshNumberWithTypicalFloors &&
                    !IsVisibilityMissingParametersMshElementOfTypicalStorey)
                {
                    IsVisibilityMissingParameters = false;
                }

                tr.Commit();
            }
            catch (Exception e)
            {
                tr.RollBack();
                MessageBox.Show(e.Message, "Ошибка");
            }
        });
    }

    [RelayCommand]
    private void LoadSharedParamMshTypeFloorElement()
    {
        ActionEventHandler.Raise(_ =>
        {
            using Transaction tr = new Transaction(_doc, "Добавить общий параметр");
            tr.Start();
            try
            {
                var isCreate = Helpers.CreateSharedParameter(_doc, ParamMshTypeFloorElement, SpecTypeId.Boolean.YesNo,
                    GroupTypeId.Data,
                    true, _mepCategories);
                if (isCreate)
                {
                    IsVisibilityMissingParametersMshElementOfTypicalStorey = false;
                }

                if (!IsVisibilityMissingParametersMshNumberWithTypicalFloors &&
                    !IsVisibilityMissingParametersMshElementOfTypicalStorey)
                {
                    IsVisibilityMissingParameters = false;
                }

                tr.Commit();
            }
            catch (Exception e)
            {
                tr.RollBack();
                MessageBox.Show(e.Message, "Ошибка");
            }
        });
    }
}