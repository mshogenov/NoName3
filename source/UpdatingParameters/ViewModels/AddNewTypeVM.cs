using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class AddNewTypeVM : ViewModelBase
{
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private string _newTypeName;
    [ObservableProperty] private List<Category> _categories = [];
    private Category _selectedCategory;
    [ObservableProperty] private UIElement _currentPopupTarget;
    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private List<Parameter> _instanceParameters = [];
    [ObservableProperty] private List<Parameter> _typeParameters = [];
    [ObservableProperty]
    private FilterGroup rootGroup;
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


    [ObservableProperty] private List<string> _parameters = [];
    [ObservableProperty] private Parameter _selectedParameter;

    public AddNewTypeVM()
    {
        RootGroup = new FilterGroup { CanRemove = false };
        Categories = GetAllCategoryByType(_doc, CategoryType.Model)
            .OrderBy(x => x.Name)
            .ToList();
    }

    private void UpdateParameters()
    {
        InstanceParameters = GetInstanceParameters(_doc, SelectedCategory.BuiltInCategory);
        TypeParameters = GetTypeParameters(_doc, SelectedCategory.BuiltInCategory);
    }

    private List<Parameter> GetTypeParameters(Document doc, BuiltInCategory builtInCategory)
    {
        // Получаем первый элемент категории
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        Element element = collector.OfCategory(builtInCategory)
            .WhereElementIsNotElementType()
            .FirstElement();
        var elementType = _doc.GetElement(element.GetTypeId());
        var typeParameters = elementType.Parameters;
        return typeParameters.Cast<Parameter>().ToList();
    }

    private List<Category> GetAllCategoryByType(Document doc, CategoryType categoryType)
    {
        Categories categories = _doc.Settings.Categories;
        return categories.Cast<Category>()
            .Where(c => c.CategoryType == categoryType).ToList();
    }

    List<Parameter> GetInstanceParameters(Document doc, BuiltInCategory builtInCategory)
    {
        // Получаем первый элемент категории
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        Element element = collector.OfCategory(builtInCategory)
            .WhereElementIsNotElementType()
            .FirstElement();
        var instanceParameters = element.Parameters;
        return instanceParameters.Cast<Parameter>().ToList();
    }

    [RelayCommand]
    private void SelectParameter(Button button)
    {
        if (button == null) return;
        // Устанавливаем целевой элемент для Popup
        CurrentPopupTarget = button;

        // Открываем Popup
        IsPopupOpen = true;
    }
    [RelayCommand]
    private void ClosePopup()
    {
        IsPopupOpen = false;
    }
}