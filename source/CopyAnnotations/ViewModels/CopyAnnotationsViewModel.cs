using System.Windows;
using CopyAnnotations.Services;
using Nice3point.Revit.Toolkit.External.Handlers;

namespace CopyAnnotations.ViewModels;

public sealed partial class CopyAnnotationsViewModel : ObservableObject
{
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
            SelectedTagRefs = _copyAnnotationsServices.GetCopyTags().ToList();
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
            if (SourceBasePoint != null)
            {
                IsBasePointSet = true;
                BasePointStatusTooltip = "Базовая точка установлена";
            }
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