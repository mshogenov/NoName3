using System.Windows;
using Autodesk.Revit.UI;
using CopyAnnotations.Services;
using Nice3point.Revit.Toolkit.External.Handlers;
using NoNameApi.Utils;

namespace CopyAnnotations.ViewModels;

public sealed partial class CopyAnnotationsViewModel : ObservableObject
{
    private readonly Document? _doc = Context.ActiveDocument;
    private readonly UIDocument? _uiDoc = Context.ActiveUiDocument;
    private List<Reference> _selectedTagRefs = [];
    [ObservableProperty] private int _selectedTagRefsCount;
    [ObservableProperty] private bool _isBasePointSet;
    [ObservableProperty] private string _basePointStatusTooltip = "Базовая точка не установлена";

    private List<Reference> SelectedTagRefs
    {
        get => _selectedTagRefs;
        set
        {
            if (Equals(value, _selectedTagRefs)) return;
            _selectedTagRefs = value ?? throw new ArgumentNullException(nameof(value));
            OnPropertyChanged();
            CopyAnnotationsCommand.NotifyCanExecuteChanged();
        }
    }

    private XYZ? SourceBasePoint
    {
        get => _sourceBasePoint;
        set
        {
            if (Equals(value, _sourceBasePoint)) return;
            _sourceBasePoint = value;
            OnPropertyChanged();
            CopyAnnotationsCommand.NotifyCanExecuteChanged();
        }
    }

    private readonly ActionEventHandler _actionEventHandler = new();

    private XYZ? _sourceBasePoint;
    private readonly CopyAnnotationsServices _copyAnnotationsServices;

    public CopyAnnotationsViewModel()
    {
        _copyAnnotationsServices = new CopyAnnotationsServices();
    }

    [RelayCommand]
    private void SelectedAnnotations(object parameter)
    {
        if (parameter is not Window window) return;
        try
        {
            window.Hide();
            var selectedElements = Helpers.GetSelectedElements(_uiDoc)
                .Where(x => x is IndependentTag or TextNote or AnnotationSymbol).ToList();
            SelectedTagRefs = selectedElements.Any()
                ? selectedElements.Select(x => new Reference(x)).ToList()
                : _copyAnnotationsServices.GetCopyTags().ToList();

            if (SelectedTagRefs.Any())
            {
                SelectedTagRefsCount = SelectedTagRefs.Count;
            }

            window.Show();
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
        }
    }

    [RelayCommand]
    private void SelectBasePoint()
    {
        try
        {
            SourceBasePoint = _copyAnnotationsServices.GetPoint("Выберите базовую точку копирования");
            if (SourceBasePoint == null) return;
            IsBasePointSet = true;
            BasePointStatusTooltip = "Базовая точка установлена";
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyAnnotations))]
    private void CopyAnnotations()
    {
        _actionEventHandler.Raise(_ =>
        {
            try
            {
                if (SourceBasePoint != null)
                    _copyAnnotationsServices.CopyAnnotations(SelectedTagRefs, SourceBasePoint);
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

    private bool CanCopyAnnotations()
    {
        return SelectedTagRefs.Any() && SourceBasePoint != null;
    }
}