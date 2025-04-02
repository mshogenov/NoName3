using System.Windows;
using Autodesk.Revit.UI;
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
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private string _statusText = "Готов к работе";
    private const string _parameterRupture = "msh_Разрыв";
    // Добавьте поле для отслеживания состояния выполнения команды
    private bool _isExecutingMakeBreak = false;

    [ObservableProperty] private string _statusColor = "#3498DB";

    private readonly List<BuiltInCategory> _categories =
    [
        BuiltInCategory.OST_PipeCurves,
    ];

// Метод для обновления статуса
    public void UpdateStatus(string message, bool isSuccess)
    {
        StatusText = message;
        StatusColor = isSuccess ? "#2ECC71" : "#E74C3C";
    }

    public MakeBreakViewModel()
    {
        _familySymbol = _makeBreakServices.FindFamily("Разрыв");
        if (!Helpers.CheckParameterExists(_doc, _parameterRupture))
        {
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
        }
    }

    [RelayCommand]
    private void BringBackVisibilityPipe()
    {
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
}