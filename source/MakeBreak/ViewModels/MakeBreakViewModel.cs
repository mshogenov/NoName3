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
    private View _activeView;

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

    [ObservableProperty] private string _statusColor = "#3498DB";

    private readonly List<BuiltInCategory> _categories =
    [
        BuiltInCategory.OST_PipeCurves,
    ];

    private bool _isExecutingDeleteBreak;


    public MakeBreakViewModel()
    {
        _familySymbol = _makeBreakServices.FindFamily("Разрыв");
        if (Helpers.CheckParameterExists(_doc, _parameterRupture)) return;
        using Transaction tr = new Transaction(_doc, "Добавить общий параметр");
        tr.Start();
        try
        {
            var isCreate = Helpers.CreateSharedParameter(_doc, _parameterRupture,
                SpecTypeId.Boolean.YesNo, GroupTypeId.Graphics, true, _categories);
            tr.Commit();
        }
        catch (Exception e)
        {
            tr.RollBack();
            MessageBox.Show(e.Message, "Ошибка");
        }
   
        uiapp.ViewActivated += OnViewActivated;
    }
    private void OnViewActivated(object sender, ViewActivatedEventArgs e)
    {
        Document doc = e.Document;
        View activeView = e.CurrentActiveView;
     // проверка наличия фильтра на виде
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
                _makeBreakServices.DeleteBreaksAndCreatePipe(_familySymbol);
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