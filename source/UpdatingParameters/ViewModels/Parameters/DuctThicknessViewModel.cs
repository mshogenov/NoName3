using System.Collections.ObjectModel;
using System.Windows;
using UpdatingParameters.Models;
using UpdatingParameters.Storages.Parameters;
using DuctParameterDialog = UpdatingParameters.Views.Parameters.DuctParameterDialog;

namespace UpdatingParameters.ViewModels.Parameters;

public sealed partial class DuctThicknessViewModel : ObservableObject
{
    private readonly DuctParametersDataStorage _ductParametersDataStorage;
    private ObservableCollection<DuctParameters> _ductParameters;


    public ObservableCollection<DuctParameters> DuctParameters
    {
        get => _ductParameters;
        private set
        {
            _ductParameters = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    private DuctParameters _selectedParameter;


    public DuctThicknessViewModel(DuctParametersDataStorage ductParametersDataStorage)
    {
        _ductParametersDataStorage = ductParametersDataStorage;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var parameters = _ductParametersDataStorage.DuctParameters;
            DuctParameters = new ObservableCollection<DuctParameters>(parameters);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private bool CanCopy()
    {
        return SelectedParameter != null;
    }

    [RelayCommand(CanExecute = nameof(CanCopy))]
    private void Copy()
    {
        if (SelectedParameter == null) return;
        int nextId = _ductParametersDataStorage.DuctParameters.Any()
            ? _ductParametersDataStorage.DuctParameters.Max(p => p.Id) + 1
            : 1;
        // Создаем новый объект с теми же данными
        var newParameter = new DuctParameters
        {
            Id = nextId,
            Material = SelectedParameter.Material,
            Shape = SelectedParameter.Shape,
            ExternalInsulation = SelectedParameter.ExternalInsulation,
            InternalInsulation = SelectedParameter.InternalInsulation,
            Size = SelectedParameter.Size,
            Thickness = SelectedParameter.Thickness
        };

        // Добавляем в базу данных
        _ductParametersDataStorage.Add(newParameter);
        LoadData();
    }

    [RelayCommand]
    private void Add()
    {
        int nextId = _ductParametersDataStorage.DuctParameters.Any()
            ? _ductParametersDataStorage.DuctParameters.Max(p => p.Id) + 1
            : 1;

        var newParameter = new DuctParameters
        {
            Id = nextId
        };
        var dialog = new DuctParameterDialog(newParameter);

        if (dialog.ShowDialog() == true)
        {
            _ductParametersDataStorage.Add(newParameter);
            LoadData();
        }
    }

    [RelayCommand]
    private void Update()
    {
        if (SelectedParameter == null) return;

        var dialog = new DuctParameterDialog(SelectedParameter);
        if (dialog.ShowDialog() == true)
        {
            _ductParametersDataStorage.Update(SelectedParameter);
            LoadData();
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedParameter == null) return;

        if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _ductParametersDataStorage.Delete(SelectedParameter.Id);
            LoadData();
        }
    }

    [RelayCommand]
    private void Save()
    {
        _ductParametersDataStorage.Save();
        MessageBox.Show("Данные сохранены");
    }
}