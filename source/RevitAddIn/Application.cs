using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Nice3point.Revit.Toolkit.External;
using RevitAddIn.Commands.CreatingSchematicsCommands;
using RevitAddIn.Commands.CreatingSpecificationsCommands;
using RevitAddIn.Commands.SystemModelingCommands;
using RevitAddIn.Services;
using UpdatingParameters.Services;

namespace RevitAddIn
{
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        private FailureReplacement _failureReplacement;

        public static List<ElementId> SelectionHistory { get;  } = [];

        public override void OnStartup()
        {
            CreateRibbon();
            RegisterUpdaterParameters();
            _failureReplacement = new FailureReplacement();
            // Application.ControlledApplication.FailuresProcessing += ControlledOnFailuresProcessing;
            Application.SelectionChanged += LastAllocation;
        }

        private void LastAllocation(object? sender, SelectionChangedEventArgs e)
        {
           
            ICollection<ElementId> currentSelection = e.GetSelectedElements();
            switch (currentSelection.Count)
            {
                case 0:
                    return;
                case > 1:
                {
                    SelectionHistory.Clear();
                    // Добавление текущего выделения в историю
                    foreach (ElementId id in currentSelection)
                    {
                        if (!SelectionHistory.Contains(id))
                        {
                            SelectionHistory.Add(id);
                        }
                    }
                    break;
                }
            }
        }
      

        private void ControlledOnFailuresProcessing(object sender, FailuresProcessingEventArgs e)
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

        private void CreateRibbon()
        {
            var panelSystemModeling = Application.CreatePanel("Моделирование", "Фигня");
            var panelSystemCreatingSchematics = Application.CreatePanel("Схемы", "Фигня");
            var panelFormationOfSpecification = Application.CreatePanel("Спецификации", "Фигня");

            #region Bloom

            var bloomCommandButton = panelSystemModeling.AddPushButton<BloomCommand>("Вставить трубу")
                .SetImage("/RevitAddIn;component/Resources/Icons/Bloom16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Bloom32.png");
            ((PushButton)bloomCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

            #endregion

            #region Tap

            var tapCommandButton = panelSystemModeling.AddPushButton<TapCommand>("Врезать")
                .SetImage("/RevitAddIn;component/Resources/Icons/Tap16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Tap32.ico");
            ((PushButton)tapCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

            #endregion

            #region Elbow

            SplitButtonData splitButtonDataElbow = new("sds", "sdsa");
            SplitButton? splitButtonElbow = panelSystemModeling.AddItem(splitButtonDataElbow) as SplitButton;

            if (splitButtonElbow != null)
            {
                var elbowDownCommandButton = splitButtonElbow.AddPushButton<ElbowDownCommand>("Поворот вниз")
                    .SetImage("/RevitAddIn;component/Resources/Icons/ElbowDown16.ico")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/ElbowDown32.png");
                ((PushButton)elbowDownCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
            }

            if (splitButtonElbow != null)
            {
                var elbowUpCommandButton = splitButtonElbow.AddPushButton<ElbowUpCommand>("Поворот вверх")
                    .SetImage("/RevitAddIn;component/Resources/Icons/ElbowUp16.ico")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/ElbowUp32.png");
                ((PushButton)elbowUpCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
            }

            if (splitButtonElbow != null)
            {
                var elbowLeftCommandButton = splitButtonElbow.AddPushButton<ElbowLeftCommand>("Поворот влево")
                    .SetImage("/RevitAddIn;component/Resources/Icons/ElbowLeft16.ico")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/ElbowLeft32.png");
                ((PushButton)elbowLeftCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
            }

            if (splitButtonElbow != null)
            {
                var elbowRightCommandButton = splitButtonElbow.AddPushButton<ElbowRightCommand>("Поворот вправо")
                    .SetImage("/RevitAddIn;component/Resources/Icons/ElbowRight16.ico")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/ElbowRight32.png");
                ((PushButton)elbowRightCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
            }

            if (splitButtonElbow != null)
            {
                var elbowDownFortyFiveCommandButton = splitButtonElbow
                    .AddPushButton<ElbowDownFortyFiveCommand>("Поворот вниз на 45°")
                    .SetImage("/RevitAddIn;component/Resources/Icons/ElbowDown45 16.ico")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/ElbowDown45 32.png");
                ((PushButton)elbowDownFortyFiveCommandButton).AvailabilityClassName =
                    typeof(CommandAvailability).FullName;
            }

            if (splitButtonElbow != null)
            {
                var elbowUpFortyFiveCommandButton = splitButtonElbow
                    .AddPushButton<ElbowUpFortyFiveCommand>("Поворот вверх на 45°")
                    .SetImage("/RevitAddIn;component/Resources/Icons/elbowUp45-16.ico")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/elbowUp45-32.ico");
                ((PushButton)elbowUpFortyFiveCommandButton).AvailabilityClassName =
                    typeof(CommandAvailability).FullName;
            }

            #endregion

            #region Align

            var threeDeeBranchAlignLiteCommandButton = panelSystemModeling
                .AddPushButton<ThreeDeeBranchAlignLiteCommand>("Выровнить оси")
                .SetImage("/RevitAddIn;component/Resources/Icons/BranchAlignLite16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/BranchAlignLite32.png");
            ((PushButton)threeDeeBranchAlignLiteCommandButton).AvailabilityClassName =
                typeof(CommandAvailability).FullName;

            #endregion

            #region Connect

            var moveConnectAlignCommandButton = panelSystemModeling.AddPushButton<MoveConnectAlignCommand>("Соединить")
                .SetImage("/RevitAddIn;component/Resources/Icons/MoveConnectAlign16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/MoveConnectAlign32.png");
            ((PushButton)moveConnectAlignCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

            #endregion
            #region CopyElements

            var mepElementsCopyCommandButton = panelSystemModeling.AddPushButton<MepElementsCopyCommand>("Копировать\nэлементы")
                .SetImage("/RevitAddIn;component/Resources/Icons/dublikat_16.png")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/dublikat_32.png");
            ((PushButton)mepElementsCopyCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;

            #endregion

            
            #region LastAllocation

            panelSystemModeling.AddPushButton<LastAllocationCommand>("Последнее\nвыделенное")
                .SetImage("/RevitAddIn;component/Resources/Icons/Последнее выделенное 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Последнее выделенное 32.ico");
            #endregion
            #region RoomsInSpaces

            panelSystemModeling.AddPushButton<RoomsInSpacesCommand>("Помещения в\nпространства")
                .SetImage("/RevitAddIn;component/Resources/Icons/Помещения в пространства 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Помещения в пространства 32.ico");

            #endregion

            #region ViewSystems

            SplitButtonData splitButtonDataViewSystems = new("dgfg", "sуццц");
            SplitButton? splitButtonViewSystems =
                panelSystemCreatingSchematics.AddItem(splitButtonDataViewSystems) as SplitButton;
            if (splitButtonViewSystems != null)
            {
                var viewOfPipeSystemsCommandButton = splitButtonViewSystems
                    .AddPushButton<ViewOfPipeSystemsCommand>("Создать\nвиды систем")
                    .SetImage("/RevitAddIn;component/Resources/Icons/Pipe systems 16.png")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/Pipe systems 32.png");
                ((PushButton)viewOfPipeSystemsCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
            }

            if (splitButtonViewSystems != null)
            {
                var updateViewsCommandButton = splitButtonViewSystems.AddPushButton<UpdateViewsCommand>("Обновить виды")
                    .SetImage("/RevitAddIn;component/Resources/Icons/Обновить виды_16.png")
                    .SetLargeImage("/RevitAddIn;component/Resources/Icons/Обновить виды_32.png");
                ((PushButton)updateViewsCommandButton).AvailabilityClassName = typeof(CommandAvailability).FullName;
            }

            #endregion

            #region DesignationOfRisers

            // var designationOfRisersCommandButton = panelSystemCreatingSchematics
            //     .AddPushButton<DesignationOfRisersCommand>("Обозначение\nстояков")
            //     .SetImage("/RevitAddIn;component/Resources/Icons/Обозначение стояка_16.ico")
            //     .SetLargeImage("/RevitAddIn;component/Resources/Icons/Обозначение стояка_32.ico");

            #endregion
            #region MakeBreak

            var makeBreakCommandButton = panelSystemCreatingSchematics
                .AddPushButton<MakeBreakCommand>("Сделать разрыв")
                .SetImage("/RevitAddIn;component/Resources/Icons/Разрыв-16.png")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Разрыв-16.png");

            #endregion

            #region NumberingOfRisers

            panelSystemCreatingSchematics.AddPushButton<NumberingOfRisersCommand>("Нумерация\nстояков")
                .SetImage("/RevitAddIn;component/Resources/Icons/Нумерация стояков 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Нумерация стояков 32.ico");

            #endregion

            #region UpdatingParameters

            panelFormationOfSpecification.AddPushButton<UpdatingParametersCommand>("Обновить\nпараметры")
                .SetImage("/RevitAddIn;component/Resources/Icons/update 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/update 32.ico");

            #endregion

            #region PositionNumberingCommand

            panelFormationOfSpecification.AddPushButton<PositionNumberingCommand>("Нумерация\nпо позиции")
                .SetImage("/RevitAddIn;component/Resources/Icons/Нумерация по позиции 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Нумерация по позиции 32.ico");

            #endregion

            #region Marking

            panelSystemCreatingSchematics.AddPushButton<MarkingCommand>("Поставить\nотметки высоты")
                .SetImage("/RevitAddIn;component/Resources/Icons/Отметка высоты 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Отметка высоты 32.ico");

            #endregion

            #region ElementsTypicalFloor

            panelFormationOfSpecification.AddPushButton<ElementsTypicalFloorCommand>("Элементы\nтипового этажа")
                .SetImage("/RevitAddIn;component/Resources/Icons/Элементы типового этажа 16.ico")
                .SetLargeImage("/RevitAddIn;component/Resources/Icons/Элементы типового этажа 32.ico");

            #endregion
        }

        private void RegisterUpdaterParameters()
        {
            var parametersUpdater = new ParametersUpdater();
            UpdaterRegistry.RegisterUpdater(parametersUpdater, true);
            var updaterId = parametersUpdater.GetUpdaterId();
            var filter = new ElementClassFilter(typeof(MEPCurve));
            UpdaterRegistry.AddTrigger(updaterId, filter, Element.GetChangeTypeGeometry());
            UpdaterRegistry.AddTrigger(updaterId, filter, Element.GetChangeTypeElementAddition());
        }
    }
}