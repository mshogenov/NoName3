using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using UpdatingParameters.Models;
using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Parameters;
using FormulaManager = UpdatingParameters.Services.FormulaManager;

namespace UpdatingParameters.ViewModels;

public abstract partial class ElementTypeViewModelBase : ViewModelBase
{
    protected readonly Document Doc = Context.ActiveDocument;
    protected readonly DataStorageFormulas DataStorageFormulas;
    protected readonly List<Element> Elements;
    private readonly Element _element;
    [ObservableProperty] private ObservableCollection<Parameter> _parametersForName = [];
    [ObservableProperty] private ObservableCollection<Parameter> _parametersForNote = [];
    [ObservableProperty] private ObservableCollection<string> _parametersForQuantity = [];
    [ObservableProperty] private int _elementCounts;
    private readonly FormulaManager _nameFormulaManager;
    private readonly FormulaManager _noteFormulaManager;
    private readonly FormulaManager _quantityFormulaManager;
    [ObservableProperty] private ObservableCollection<Formula> _adskNameFormulas;
    [ObservableProperty] private ObservableCollection<Formula> _adskNoteFormulas;
    private ObservableCollection<Formula> _adskQuantityFormulas;

    public ObservableCollection<Formula> AdskQuantityFormulas
    {
        get
        {
            foreach (var adskQuantityFormula in _adskQuantityFormulas)
            {
                UpdateSignificance(adskQuantityFormula);
            }

            return _adskQuantityFormulas;
        }
        set
        {
            _adskQuantityFormulas = value;
            OnPropertyChanged();
        }
    }

    private Parameter _selectParameterName;

    public Parameter SelectParameterName
    {
        get => _selectParameterName;
        set
        {
            if (Equals(value, _selectParameterName)) return;
            _selectParameterName = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty] private Parameter _selectParameterNote;
    [ObservableProperty] private string _selectParameterQuantity;


    private bool _noteIsChecked;

    public bool NoteIsChecked
    {
        get => _noteIsChecked;
        set
        {
            _noteIsChecked = value;
            OnPropertyChanged();
            DataStorageFormulas.NoteIsChecked = value;
        }
    }

    private bool _nameIsChecked;

    public bool NameIsChecked
    {
        get => _nameIsChecked;
        set
        {
            _nameIsChecked = value;
            OnPropertyChanged();
            DataStorageFormulas.NameIsChecked = value;
        }
    }

    private bool _quantityIsChecked;

    public bool QuantityIsChecked
    {
        get => _quantityIsChecked;
        set
        {
            _quantityIsChecked = value;
            OnPropertyChanged();
            DataStorageFormulas.QuantityIsChecked = value;
        }
    }

    [ObservableProperty] private Formula _selectFormulaName;
    [ObservableProperty] private Formula _selectFormulaNote;
    [ObservableProperty] private Formula _selectFormulaQuantity;

    [ObservableProperty] private ICollectionView _parametersViewForName;
    [ObservableProperty] private ICollectionView _parametersViewForNote;
    private string _searchParametersViewForName;

    public string SearchParametersViewForName
    {
        get => _searchParametersViewForName;
        set
        {
            _searchParametersViewForName = value;
            OnPropertyChanged();
            ParametersViewForName.Refresh();
        }
    }

    private string _searchParametersViewForNote;

    public string SearchParametersViewForNote
    {
        get => _searchParametersViewForNote;
        set
        {
            _searchParametersViewForNote = value;
            OnPropertyChanged();
            ParametersViewForNote.Refresh();
        }
    }

    public string CombinedNameValues
    {
        get { return string.Concat(AdskNameFormulas.Select(f => $"{f.Prefix}{f.Significance}{f.Suffix}")); }
    }


    public string CombinedNoteValues
    {
        get { return string.Concat(AdskNoteFormulas.Select(f => $"{f.Prefix}{f.Significance}{f.Suffix}")); }
    }

    public string CombinedQuantityValues => FormationCombinedQuantityValues();

    protected ElementTypeViewModelBase(DataStorageFormulas dataStorageFormulas)
    {
        DataStorageFormulas = dataStorageFormulas;
        SettingsManager.OnSettingsChanged += SettingsManager_OnSettingsChanged;
        Elements = DataStorageFormulas.GetElements();
        _elementCounts = Elements.Count;
        _element = Elements.FirstOrDefault(x => x.IsValidObject);
        AdskNameFormulas = new ObservableCollection<Formula>(DataStorageFormulas.NameFormulas);
        AdskNoteFormulas = new ObservableCollection<Formula>(DataStorageFormulas.NoteFormulas);
        AdskQuantityFormulas = new ObservableCollection<Formula>(DataStorageFormulas.QuantityFormulas);
        NoteIsChecked = DataStorageFormulas.NoteIsChecked;
        NameIsChecked = DataStorageFormulas.NameIsChecked;
        QuantityIsChecked = DataStorageFormulas.QuantityIsChecked;
        var nameFormulasHandler = new FormulaCollectionHandler(AdskNameFormulas);
        var noteFormulasHandler = new FormulaCollectionHandler(AdskNoteFormulas);
        var quantityFormulasHandler = new FormulaCollectionHandler(AdskQuantityFormulas);
        nameFormulasHandler.OnCollectionChangedAction += () => OnPropertyChanged(nameof(CombinedNameValues));
        noteFormulasHandler.OnCollectionChangedAction += () => OnPropertyChanged(nameof(CombinedNoteValues));
        quantityFormulasHandler.OnCollectionChangedAction += () => OnPropertyChanged(nameof(CombinedQuantityValues));
        quantityFormulasHandler.OnCollectionChangedAction += () => OnPropertyChanged(nameof(AdskQuantityFormulas));
        var ignoredParametersName = DataStorageFormulas.NameFormulas.Select(x => x.ParameterName).ToList();
        var ignoredParametersNote = DataStorageFormulas.NoteFormulas.Select(x => x.ParameterName).ToList();
        var ignoredParametersQuantity = DataStorageFormulas.QuantityFormulas.Select(x => x.ParameterName).ToList();
        var parameters = UpdaterParametersService.GetAllParameters(_element);
        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                if (!ignoredParametersName.Contains(parameter.Definition.Name) &&
                    ParametersForName.All(p => p.Definition.Name != parameter.Definition.Name))
                {
                    ParametersForName.Add(parameter);
                }

                if (!ignoredParametersQuantity.Contains(parameter.Definition.Name))
                {
                    if (parameter.Definition.Name == "Длина")
                    {
                        ParametersForQuantity.Add(parameter.Definition.Name);
                    }
                }

                if (!ignoredParametersNote.Contains(parameter.Definition.Name) &&
                    ParametersForNote.All(p => p.Definition.Name != parameter.Definition.Name))
                {
                    ParametersForNote.Add(parameter);
                }

                if (_element?.Category.Id.Value != (long)BuiltInCategory.OST_PipeInsulations) continue;
                if (ignoredParametersQuantity.Contains(parameter.Definition.Name)) continue;
                switch (parameter.Definition.Name)
                {
                    case "Площадь":
                    case "Объем":
                        ParametersForQuantity.Add(parameter.Definition.Name);
                        break;
                }
            }

            if (_element is FlexPipe || _element?.Category.Id.Value == (long)BuiltInCategory.OST_PipeInsulations)
            {
                if (!ignoredParametersQuantity.Contains("Число"))
                {
                    ParametersForQuantity.Add("Число");
                }
            }
        }

        ParametersViewForName = CreateParametersView(ParametersForName, FilterParametersForName);
        ParametersViewForNote = CreateParametersView(ParametersForNote, FilterParametersForNote);
        _nameFormulaManager = new FormulaManager(AdskNameFormulas, ParametersForName,
            () => DataStorageFormulas.NameFormulas = AdskNameFormulas);
        _noteFormulaManager = new FormulaManager(AdskNoteFormulas, ParametersForNote,
            () => DataStorageFormulas.NoteFormulas = AdskNoteFormulas);
        _quantityFormulaManager = new FormulaManager(AdskQuantityFormulas, ParametersForQuantity,
            () => DataStorageFormulas.QuantityFormulas = AdskQuantityFormulas);
    }

    private string FormationCombinedQuantityValues()
    {
        StringBuilder combinedQuantityValues = new StringBuilder();

        if (AdskQuantityFormulas != null)
        {
            foreach (var adskQuantityFormula in AdskQuantityFormulas)
            {
                // Нормализуем строки: заменяем запятые на точки
                string normalizedSignificance = adskQuantityFormula.Significance.Replace(',', '.');
                string normalizedStockpile = adskQuantityFormula.Stockpile.Replace(',', '.');

                // Пытаемся парсить с использованием InvariantCulture
                bool parsedSignificance = double.TryParse(
                    normalizedSignificance,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double significance
                );

                bool parsedStockpile = double.TryParse(normalizedStockpile, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double stockpile);

                if (parsedSignificance && parsedStockpile)
                {
                    double product = significance * stockpile;
                    combinedQuantityValues.Append(product.ToString(CultureInfo.InvariantCulture));
                }
                else if (parsedSignificance)
                {
                    combinedQuantityValues.Append(significance.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    // Обработка случая, когда парсинг не удался
                    // Например, можно пропустить или добавить значение по умолчанию
                    // Здесь добавляем ноль
                    combinedQuantityValues.Append("0");
                }

                // Добавляем разделитель, например, запятую (можно выбрать другой)
                combinedQuantityValues.Append(", ");
            }
        }

        // Удаляем последний разделитель, если необходимо
        if (combinedQuantityValues.Length > 2)
        {
            combinedQuantityValues.Length -= 2;
        }

        return combinedQuantityValues.ToString();
    }

    private void SettingsManager_OnSettingsChanged()
    {
        DataStorageFormulas.LoadData();
        AdskNameFormulas.Clear();
        foreach (var formula in DataStorageFormulas.NameFormulas)
        {
            AdskNameFormulas.Add(formula);
        }

        AdskNoteFormulas.Clear();
        foreach (var formula in DataStorageFormulas.NoteFormulas)
        {
            AdskNoteFormulas.Add(formula);
        }

        AdskQuantityFormulas.Clear();
        foreach (var formula in DataStorageFormulas.QuantityFormulas)
        {
            AdskQuantityFormulas.Add(formula);
        }

        NoteIsChecked = DataStorageFormulas.NoteIsChecked;
        NameIsChecked = DataStorageFormulas.NameIsChecked;
        QuantityIsChecked = DataStorageFormulas.QuantityIsChecked;
    }

    private ICollectionView CreateParametersView(IEnumerable<Parameter> parameters, Predicate<object> filter)
    {
        var view = CollectionViewSource.GetDefaultView(parameters);
        view.SortDescriptions.Add(new SortDescription("Definition.Name", ListSortDirection.Ascending));
        view.Filter = filter;
        return view;
    }

    private bool FilterParametersForName(object obj)
    {
        if (obj is not Parameter parameter) return false;
        return string.IsNullOrWhiteSpace(SearchParametersViewForName) ||
               parameter.Definition.Name.Contains(SearchParametersViewForName, StringComparison.OrdinalIgnoreCase);
    }


    private bool FilterParametersForNote(object obj)
    {
        if (obj is not Parameter parameter) return false;
        return string.IsNullOrWhiteSpace(SearchParametersViewForNote) ||
               parameter.Definition.Name.Contains(SearchParametersViewForNote, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSignificance(Formula formula)
    {
        if (formula == null) return;

        Parameter parameter = _element.FindParameter(formula.ParameterName);
        formula.Significance = formula.MeasurementUnit switch
        {
            MeasurementUnit.Meter => parameter?.AsDouble().ToMeters().ToString("F3", CultureInfo.CurrentCulture),
            MeasurementUnit.Millimeter => parameter?.AsDouble().ToMillimeters()
                .ToString("F3", CultureInfo.CurrentCulture),
            MeasurementUnit.CubicMeter => parameter?.AsDouble().ToUnit(UnitTypeId.CubicMeters)
                .ToString("F3", CultureInfo.CurrentCulture),
            MeasurementUnit.SquareMeters => parameter?.AsDouble().ToUnit(UnitTypeId.SquareMeters)
                .ToString("F3", CultureInfo.CurrentCulture),
            MeasurementUnit.Piece => "1",
            _ => ""
        } ?? string.Empty;
    }

    #region ADSK_Наименование

    [RelayCommand]
    private void AddParameterToNameFormula()
    {
        _nameFormulaManager.AddParameter(SelectParameterName);
    }

    [RelayCommand]
    private void RemoveParameterToNameFormula()
    {
        var formulaToRemove = SelectFormulaName;
        _nameFormulaManager.RemoveParameter(formulaToRemove, _element);
    }

    [RelayCommand]
    private void MoveUpToNameFormula()
    {
        var selectFormula = SelectFormulaName;
        _nameFormulaManager.MoveUp(selectFormula, () =>
        {
            /* Дополнительные действия при необходимости */
        });
    }

    [RelayCommand]
    private void MoveDownToNameFormula()
    {
        var selectFormula = SelectFormulaName;
        _nameFormulaManager.MoveDown(selectFormula, () =>
        {
            /* Дополнительные действия при необходимости */
        });
    }

    #endregion

    #region ADSK_Примечание

    [RelayCommand]
    private void AddParameterToNoteFormula()
    {
        _noteFormulaManager.AddParameter(SelectParameterNote);
    }

    [RelayCommand]
    private void RemoveParameterToNoteFormula()
    {
        var formulaToRemove = SelectFormulaNote;
        _noteFormulaManager.RemoveParameter(formulaToRemove, _element);
    }

    [RelayCommand]
    private void MoveUpToNoteFormula()
    {
        var selectFormula = SelectFormulaNote;
        _noteFormulaManager.MoveUp(selectFormula, () =>
        {
            /* Дополнительные действия при необходимости */
        });
    }

    [RelayCommand]
    private void MoveDownToNoteFormula()
    {
        var selectFormula = SelectFormulaNote;
        _noteFormulaManager.MoveDown(selectFormula, () =>
        {
            /* Дополнительные действия при необходимости */
        });
    }

    #endregion

    #region ADSK_Количество

    [RelayCommand]
    private void AddParameterToQuantityFormula()
    {
        if (AdskQuantityFormulas.Count == 0)
        {
            _quantityFormulaManager.AddParameterQuantity(SelectParameterQuantity, _element);
        }
    }

    [RelayCommand]
    private void RemoveParameterToQuantityFormula()
    {
        var formulaToRemove = SelectFormulaQuantity;
        _quantityFormulaManager.RemoveParameterQuantity(formulaToRemove);
    }

    [RelayCommand]
    private void MoveUpToQuantityFormula()
    {
        var selectFormula = SelectFormulaQuantity;
        _quantityFormulaManager.MoveUp(selectFormula, () =>
        {
            /* Дополнительные действия при необходимости */
        });
    }

    [RelayCommand]
    private void MoveDownToQuantityFormula()
    {
        var selectFormula = SelectFormulaQuantity;
        _quantityFormulaManager.MoveDown(selectFormula, () =>
        {
            /* Дополнительные действия при необходимости */
        });
    }

    #endregion

    [RelayCommand]
    protected virtual void UpdateElements(object window)
    {
        var view = window as Window;
        using Transaction tr = new(Context.ActiveDocument, "Обновление параметров");
        try
        {
            tr.Start();
            if (Elements == null || Elements.Count == 0)
            {
                tr.RollBack();
                TaskDialog.Show("Ошибка", "Нет труб для обновления.");
                UpdaterParametersService.ReturnWindowState(view);
                return;
            }

            int current = 0;
            foreach (var element in Elements)
            {
                UpdaterParametersService.UpdateParameters(element, DataStorageFormulas);
                current++;
            }

            tr.Commit();
            TaskDialog.Show("Обновление элементов", $"Обновлено элементов: {current}");
            UpdaterParametersService.ReturnWindowState(view);
        }
        catch (Exception ex)
        {
            tr.RollBack();
            TaskDialog.Show("Ошибка", ex.Message);
            UpdaterParametersService.ReturnWindowState(view);
        }
    }


    [RelayCommand]
    private void SaveSettings()
    {
        DataStorageFormulas.Save();
        MessageBox.Show("Настройки сохранены");
    }
}