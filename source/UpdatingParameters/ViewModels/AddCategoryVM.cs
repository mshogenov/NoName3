using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.Models;
using UpdatingParameters.Views;

namespace UpdatingParameters.ViewModels;

public partial class AddCategoryVM : ViewModelBase
{
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private List<Category> _categories = [];
    [ObservableProperty] private List<Parameter> _instanceParameters = [];
    [ObservableProperty] private List<Parameter> _typeParameters = [];
    [ObservableProperty] private UIElement _fromParameterPopupTarget;
    [ObservableProperty] private bool _isFromParameterPopupOpen;
    [ObservableProperty] private UIElement _inParameterPopupTarget;
    [ObservableProperty] private bool _isInParameterPopupOpen;
    [ObservableProperty] private double _margin;

    [ObservableProperty] private Parameter _selectedInParameter;
    private Category _selectedCategory;

    public Category SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (Equals(value, _selectedCategory)) return;
            _selectedCategory = value;
            UpdateParameters();
            OnPropertyChanged();
        }
    }

    private Parameter _selectedFromParameter;

    public Parameter SelectedFromParameter
    {
        get => _selectedFromParameter;
        set
        {
            if (Equals(value, _selectedFromParameter)) return;
            _selectedFromParameter = value;
            OnPropertyChanged();
        }
    }

    public MarginCategory Result { get; private set; }
    public bool IsConfirmed { get; private set; }
    private bool _isCopyInParameter;

    public bool IsCopyInParameter
    {
        get => _isCopyInParameter;
        set
        {
            if (value == _isCopyInParameter) return;
            _isCopyInParameter = value;
            if (value && SelectedFromParameter != null)
            {
                foreach (var instanceParameter in InstanceParameters.Where(instanceParameter =>
                             instanceParameter.Definition.Name == SelectedFromParameter.Definition.Name))
                {
                    InstanceParameters.Remove(instanceParameter);
                    break;
                }

                foreach (var typeParameters in TypeParameters.Where(typeParameters =>
                             typeParameters.Definition.Name == SelectedFromParameter.Definition.Name))
                {
                    TypeParameters.Remove(typeParameters);
                    break;
                }
            }

            OnPropertyChanged();
        }
    }

    public AddCategoryVM(List<MarginCategory> marginCategories)
    {
        Categories = GetAllCategoryByType(CategoryType.Model).Where(x =>
            {
                return marginCategories.All(marginCategory => marginCategory.CategoryName != x.Name);
            })
            .OrderBy(x => x.Name)
            .ToList();
    }

    [RelayCommand]
    private void SelectFromParameter(Button button)
    {
        if (button == null) return;
        // Устанавливаем целевой элемент для Popup
        FromParameterPopupTarget = button;

        // Открываем Popup
        IsFromParameterPopupOpen = true;
    }

    [RelayCommand]
    private void SelectInParameter(Button button)
    {
        if (button == null) return;
        InParameterPopupTarget = button;
        IsInParameterPopupOpen = true;
    }

    [RelayCommand]
    private void CloseFromParameterPopup()
    {
        IsFromParameterPopupOpen = false;
    }

    [RelayCommand]
    private void CloseInParameterPopup()
    {
        IsInParameterPopupOpen = false;
    }

    private List<Category> GetAllCategoryByType(CategoryType categoryType)
    {
        Categories categories = _doc.Settings.Categories;
        return categories.Cast<Category>()
            .Where(c => c.CategoryType == categoryType)
            .ToList();
    }

    private void UpdateParameters()
    {
        InstanceParameters = GetInstanceParameters(_doc, SelectedCategory.BuiltInCategory);
        TypeParameters = GetTypeParameters(_doc, SelectedCategory.BuiltInCategory);
    }

    List<Parameter> GetInstanceParameters(Document doc, BuiltInCategory builtInCategory)
    {
        // Получаем первый элемент категории
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        Element element = collector.OfCategory(builtInCategory)
            .WhereElementIsNotElementType()
            .FirstElement();
        return element != null
            ? element.Parameters
                .Cast<Parameter>()
                .Where(x => x.StorageType is StorageType.Double or StorageType.Integer)
                .OrderBy(x => x.Definition.Name)
                .ToList()
            : [];
    }

    private List<Parameter> GetTypeParameters(Document doc, BuiltInCategory builtInCategory)
    {
        // Получаем первый элемент категории
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        return collector.OfCategory(builtInCategory)
            .WhereElementIsElementType()
            .FirstElement() is ElementType elementType
            ? elementType.Parameters
                .Cast<Parameter>()
                .Where(x => x.StorageType is StorageType.Double or StorageType.Integer)
                .OrderBy(x => x.Definition.Name)
                .ToList()
            : [];
    }

    [RelayCommand]
    private void Confirm(object param)
    {
        if (SelectedCategory != null && SelectedFromParameter != null)
        {
            if (IsCopyInParameter)
            {
                Result = new MarginCategory
                {
                    Category = SelectedCategory,
                    Margin = Margin,
                    FromParameter = SelectedFromParameter,
                    IsCopyInParameter = true,
                    InParameter = SelectedInParameter,
                    IsChecked = true,
                    IsFromParameterValid = true,
                    IsInParameterValid = true,
                };
            }
            else
            {
                Result = new MarginCategory
                {
                    Category = SelectedCategory,
                    Margin = Margin,
                    FromParameter = SelectedFromParameter,
                    IsCopyInParameter = false,
                    IsChecked = true,
                    IsFromParameterValid = true,
                  };
            }

            IsConfirmed = true;
            if (param is not Window window) return;
            window.DialogResult = true;
            window.Close();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsConfirmed = false;
    }
}