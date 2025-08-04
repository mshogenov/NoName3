using System.Windows;
using System.Windows.Controls;

namespace UpdatingParameters.ViewModels;

public partial class AddCategoryVM: ViewModelBase
{
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private List<Category> _categories = [];
    [ObservableProperty] private List<Parameter> _instanceParameters = [];
    [ObservableProperty] private List<Parameter> _typeParameters = [];
    [ObservableProperty] private UIElement _currentPopupTarget;
    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private Parameter _selectedParameter;
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

    public AddCategoryVM()
    {
        Categories = GetAllCategoryByType(_doc, CategoryType.Model)
            .OrderBy(x => x.Name)
            .ToList();
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
    private List<Category> GetAllCategoryByType(Document doc, CategoryType categoryType)
    {
        Categories categories = _doc.Settings.Categories;
        return categories.Cast<Category>()
            .Where(c => c.CategoryType == categoryType).ToList();
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
        var instanceParameters = element.Parameters;
        return instanceParameters.Cast<Parameter>().ToList();
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
}