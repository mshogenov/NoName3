using System.Collections.ObjectModel;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using LastAllocation.Models;
using Nice3point.Revit.Toolkit.External;
using RevitAddIn2.Commands.CreatingSchematicsCommands;
using RevitAddIn2.Commands.CreatingSpecificationsCommands;
using RevitAddIn2.Commands.Others;
using RevitAddIn2.Commands.SystemModelingCommands;
using RevitAddIn2.Services;
using UpdatingParameters.Services;

namespace RevitAddIn2;

/// <summary>
///     Application entry point
/// </summary>
[UsedImplicitly]
public class Application : ExternalApplication
{
    private RibbonPanel _modifyPanel;

    private  FailureReplacement? _failureReplacement;

    // Заменяем одиночный список историей из 10 списков
    public static ObservableCollection<SelectionHistoryData> SelectionHistories { get; } = [];
    private static int MaxHistories => 10;

    public override void OnStartup()
    {
        _failureReplacement = new FailureReplacement();
        CreateRibbon();
        RegisterUpdaterParameters();
        Application.SelectionChanged += LastAllocation;
         Application.ControlledApplication.FailuresProcessing += ControlledOnFailuresProcessing;
        // Application.SelectionChanged += OnSelectionChanged;
        // Application.ViewActivated += OnViewActivated;
    }


    public override void OnShutdown()
    {
        // Отписываемся от событий при выгрузке
        Application.SelectionChanged -= LastAllocation;
        // Application.SelectionChanged -= OnSelectionChanged;
        // Application.ViewActivated -= OnViewActivated;
    }
    private void ControlledOnFailuresProcessing(object? sender, FailuresProcessingEventArgs e)
    {
        var failuresAccessor = e.GetFailuresAccessor();
        var failureMessages = failuresAccessor.GetFailureMessages(FailureSeverity.Error);
        //.Where(x => x.GetFailureDefinitionId() == BuiltInFailures.FamilyFailures.UnexpectedFamilyChangeResultsWarning)
        //.ToList();
        if (failuresAccessor.GetTransactionName() != "Изменение типа") return;
        if (failureMessages.Any())
        {
            var failureHandlingOptions = failuresAccessor.GetFailureHandlingOptions();

            failureHandlingOptions.SetClearAfterRollback(true);

            failuresAccessor.SetFailureHandlingOptions(failureHandlingOptions);
            e.SetProcessingResult(FailureProcessingResult.ProceedWithRollBack);
        }

        var a = failureMessages.SelectMany(x => x.GetAdditionalElementIds());
        _failureReplacement.PostFailure(failureMessages.SelectMany(x => x.GetFailingElementIds()));
    }


    private void UpdatePanelVisibility()
    {
        var activeView = UiApplication.ActiveUIDocument.ActiveView;

        if (activeView is View3D || activeView.ViewType == ViewType.ThreeD)
        {
            foreach (var item in _modifyPanel.GetItems())
            {
                item.Visible = false;
            }

            _modifyPanel.Enabled = false;
            return;
        }

        bool hasSelection = false;

        // Проверяем тип вида - спецификация
        if (activeView.ViewType == ViewType.Schedule)
        {
            // Для спецификации проверяем выбранные строки
            var schedule = activeView as ViewSchedule;
            if (schedule != null)
            {
                // Получаем выбранные элементы в спецификации
                hasSelection = UiApplication.ActiveUIDocument.Selection.GetElementIds().Count > 0;
            }
        }
        else
        {
            // Для остальных видов проверяем обычное выделение
            hasSelection = UiApplication.ActiveUIDocument.Selection.GetElementIds().Count > 0;
        }

        // Обновляем видимость панели
        foreach (var item in _modifyPanel.GetItems())
        {
            item.Visible = hasSelection;
        }

        _modifyPanel.Enabled = hasSelection;
    }

    private void OnViewActivated(object? sender, ViewActivatedEventArgs e)
    {
        UpdatePanelVisibility();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        UpdatePanelVisibility();
    }

    private static void LastAllocation(object? sender, SelectionChangedEventArgs e)
    {
        ICollection<ElementId> currentSelection = e.GetSelectedElements();
        if (currentSelection.Count <= 1)
            return;

        // Создаем новый список выделения
        var newSelection = new List<ElementId>();
        foreach (ElementId id in currentSelection)
        {
            if (!newSelection.Contains(id))
            {
                newSelection.Add(id);
            }
        }

        // Проверяем, не совпадает ли новое выделение с уже существующим
        bool isDuplicate = false;
        foreach (var historyData in SelectionHistories)
        {
            var history = historyData.ElementsIds;
            if (history.Count == newSelection.Count &&
                history.All(newSelection.Contains) &&
                newSelection.All(history.Contains))
            {
                isDuplicate = true;
                break;
            }
        }

        // Если это не дубликат, добавляем в историю
        if (!isDuplicate && newSelection.Count > 0)
        {
            // Создаем новый объект SelectionHistoryData с текущим временем
            var newHistoryData = new SelectionHistoryData(newSelection);

            // Если достигли максимального количества историй, удаляем последнюю
            if (SelectionHistories.Count >= MaxHistories)
            {
                SelectionHistories.RemoveAt(SelectionHistories.Count - 1);
            }

            // Добавляем новую историю в начало списка
            SelectionHistories.Insert(0, newHistoryData);
        }
    }

    private void CreateRibbon()
    {
        var panelSystemModeling = Application.CreatePanel("Моделирование", "RevitAddIn");
        var panelSystemCreatingSchematics = Application.CreatePanel("Оформление", "RevitAddIn");
        var panelFormationOfSpecification = Application.CreatePanel("Спецификация", "RevitAddIn");
        var panelOther = Application.CreatePanel("Прочее", "RevitAddIn");
        _modifyPanel = Application.CreatePanel("RevitAddIn", "Modify");
        // Изначально скрываем панель
        _modifyPanel.Visible = false;

        #region InsertPipe

        var bloomCommandButton = panelSystemModeling.AddPushButton<InsertPipeCommand>("Вставить трубу")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Bloom16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Bloom32.png");
        ((PushButton)bloomCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

        #endregion

        #region Tap

        var tapCommandButton = panelSystemModeling.AddPushButton<TapCommand>("Врезать")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Tap16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Tap32.ico");
        ((PushButton)tapCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

        #endregion

        #region Elbow

        SplitButtonData splitButtonDataElbow = new("splitButtonDataElbow", "Отводы");
        SplitButton? splitButtonElbow = panelSystemModeling.AddItem(splitButtonDataElbow) as SplitButton;

        if (splitButtonElbow != null)
        {
            var elbowDownCommandButton = splitButtonElbow.AddPushButton<ElbowDownCommand>("Поворот вниз")
                .SetImage("/RevitAddIn2;component/Resources/Icons/ElbowDown16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/ElbowDown32.png");
            ((PushButton)elbowDownCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
        }

        if (splitButtonElbow != null)
        {
            var elbowUpCommandButton = splitButtonElbow.AddPushButton<ElbowUpCommand>("Поворот вверх")
                .SetImage("/RevitAddIn2;component/Resources/Icons/ElbowUp16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/ElbowUp32.png");
            ((PushButton)elbowUpCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
        }

        if (splitButtonElbow != null)
        {
            var elbowLeftCommandButton = splitButtonElbow.AddPushButton<ElbowLeftCommand>("Поворот влево")
                .SetImage("/RevitAddIn2;component/Resources/Icons/ElbowLeft16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/ElbowLeft32.png");
            ((PushButton)elbowLeftCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
        }

        if (splitButtonElbow != null)
        {
            var elbowRightCommandButton = splitButtonElbow.AddPushButton<ElbowRightCommand>("Поворот вправо")
                .SetImage("/RevitAddIn2;component/Resources/Icons/ElbowRight16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/ElbowRight32.png");
            ((PushButton)elbowRightCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
        }

        if (splitButtonElbow != null)
        {
            var elbowDownFortyFiveCommandButton = splitButtonElbow
                .AddPushButton<ElbowDownFortyFiveCommand>("Поворот вниз на 45°")
                .SetImage("/RevitAddIn2;component/Resources/Icons/ElbowDown45 16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/ElbowDown45 32.png");
            ((PushButton)elbowDownFortyFiveCommandButton).AvailabilityClassName =
                typeof(CommandAvailability).FullName;
        }

        if (splitButtonElbow != null)
        {
            var elbowUpFortyFiveCommandButton = splitButtonElbow
                .AddPushButton<ElbowUpFortyFiveCommand>("Поворот вверх на 45°")
                .SetImage("/RevitAddIn2;component/Resources/Icons/elbowUp45-16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/elbowUp45-32.ico");
            ((PushButton)elbowUpFortyFiveCommandButton).AvailabilityClassName =
                typeof(CommandAvailability).FullName;
        }

        #endregion

        #region Align

        var threeDeeBranchAlignLiteCommandButton = panelSystemModeling
            .AddPushButton<ThreeDeeBranchAlignLiteCommand>("Выровнить оси")
            .SetImage("/RevitAddIn2;component/Resources/Icons/BranchAlignLite16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/BranchAlignLite32.png");
        ((PushButton)threeDeeBranchAlignLiteCommandButton).AvailabilityClassName =
            typeof(CommandAvailability).FullName;

        #endregion

        #region Connect

        var moveConnectAlignCommandButton = panelSystemModeling.AddPushButton<MoveConnectAlignCommand>("Соединить")
            .SetImage("/RevitAddIn2;component/Resources/Icons/MoveConnectAlign16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/MoveConnectAlign32.png");
        ((PushButton)moveConnectAlignCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

        #endregion

        #region CopyElements

        var mepElementsCopyCommandButton = panelSystemModeling
            .AddPushButton<MepElementsCopyCommand>("Копировать\nэлементы")
            .SetImage("/RevitAddIn2;component/Resources/Icons/dublikat_16.png")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/dublikat_32.png");
        ((PushButton)mepElementsCopyCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

        #endregion

        #region RoomsInSpaces

        panelSystemModeling.AddPushButton<RoomsInSpacesCommand>("Помещения в\nпространства")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Помещения в пространства 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Помещения в пространства 32.ico");

        #endregion

        #region ViewSystems

        SplitButtonData splitButtonDataViewSystems = new("splitButtonDataViewSystems", "ViewSystems");
        SplitButton? splitButtonViewSystems =
            panelSystemCreatingSchematics.AddItem(splitButtonDataViewSystems) as SplitButton;
        if (splitButtonViewSystems != null)
        {
            var viewOfPipeSystemsCommandButton = splitButtonViewSystems
                .AddPushButton<ViewOfPipeSystemsCommand>("Создать\nвиды систем")
                .SetImage("/RevitAddIn2;component/Resources/Icons/Pipe systems 16.png")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Pipe systems 32.png");
            ((PushButton)viewOfPipeSystemsCommandButton).AvailabilityClassName =
                typeof(CommandAvailability).FullName;
        }

        if (splitButtonViewSystems != null)
        {
            var updateViewsCommandButton = splitButtonViewSystems.AddPushButton<UpdateViewsCommand>("Обновить виды")
                .SetImage("/RevitAddIn2;component/Resources/Icons/Обновить виды 16.ico")
                .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Обновить виды 32.ico");
            ((PushButton)updateViewsCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
        }

        #endregion

        #region MakeBreak

        var makeBreakCommandButton = panelSystemCreatingSchematics
            .AddPushButton<MakeBreakCommand>("Сделать разрыв")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Разрыв-16.png")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Разрыв-16.png");
        ((PushButton)makeBreakCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

        #endregion

        #region NumberingOfRisers

        panelSystemCreatingSchematics.AddPushButton<NumberingOfRisersCommand>("Нумерация\nстояков")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Нумерация стояков 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Нумерация стояков 32.ico");

        #endregion

        #region UpdatingParameters

        panelFormationOfSpecification.AddPushButton<UpdatingParametersCommand>("Обновить\nпараметры")
            .SetImage("/RevitAddIn2;component/Resources/Icons/update 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/update 32.ico");

        #endregion

        #region PositionNumberingCommand

        panelFormationOfSpecification.AddPushButton<PositionNumberingCommand>("Нумерация\nпо позиции")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Нумерация по позиции 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Нумерация по позиции 32.ico");

        #endregion

        #region Marking

        panelSystemCreatingSchematics.AddPushButton<MarkingCommand>("Поставить\nотметки высоты")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Отметка высоты 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Отметка высоты 32.ico");

        #endregion

        #region CopyAnnotations

        panelSystemCreatingSchematics.AddPushButton<CopyAnnotationsCommand>("Копировать\nаннотации")
            .SetImage("/RevitAddIn2;component/Resources/Icons/kopirovat_v2swjczhusqj_16.png")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/kopirovat_v2swjczhusqj_32.png");

        #endregion

        #region ElementsTypicalFloor

        panelFormationOfSpecification.AddPushButton<ElementsTypicalFloorCommand>("Элементы\nтипового этажа")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Элементы типового этажа 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Элементы типового этажа 32.ico");

        #endregion

        #region ShowIn3D

        panelOther.AddPushButton<ShowIn3DCommand>("Показать\nна 3D")
            .SetImage("/RevitAddIn2;component/Resources/Icons/ShowIn3D-16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/ShowIn3D-32.ico");

        #endregion

        #region LastAllocation

        panelOther.AddPushButton<LastAllocationCommand>("Последние\nвыделенные")
            .SetImage("/RevitAddIn2;component/Resources/Icons/Последнее выделенное 16.ico")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/Последнее выделенное 32.ico");

        #endregion

        #region SetNearestLevelBelow

        panelOther.AddPushButton<SetNearestLevelBelowCommand>("Установить\nбазовый уровень")
            .SetImage("/RevitAddIn2;component/Resources/Icons/uroven_mauxlwi8s01i_16.png")
            .SetLargeImage("/RevitAddIn2;component/Resources/Icons/uroven_mauxlwi8s01i_32.png");

        #endregion
    }

    private static void RegisterUpdaterParameters()
    {
        var parametersUpdater = new ParametersUpdater();
        UpdaterRegistry.RegisterUpdater(parametersUpdater, true);
        var updaterId = parametersUpdater.GetUpdaterId();

        // Создаем фильтры для разных типов элементов
        var mepCurveFilter = new ElementClassFilter(typeof(MEPCurve));
        var pipeFittingFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting);
        var ductFittingFilter = new ElementCategoryFilter(BuiltInCategory.OST_DuctFitting);

        // Объединяем фильтры
        var orFilter = new LogicalOrFilter(
            [mepCurveFilter, pipeFittingFilter, ductFittingFilter]
        );

        // Регистрация триггеров
        UpdaterRegistry.AddTrigger(updaterId, orFilter, Element.GetChangeTypeGeometry());
        UpdaterRegistry.AddTrigger(updaterId, orFilter, Element.GetChangeTypeElementAddition());
    }
}