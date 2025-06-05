using System.Windows;
using System.Windows.Controls;

namespace UpdatingParameters.ViewModels;

public partial class AddNewTypeVM : ViewModelBase
{
    private readonly Document _doc = Context.ActiveDocument;
    [ObservableProperty] private string _newTypeName;
    [ObservableProperty] private List<Category> _categories = [];
    private Category _selectedCategory;
    [ObservableProperty] private UIElement _currentPopupTarget;
    [ObservableProperty] private bool _isPopupOpen;
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
        Categories = GetAllCategoryByType(_doc, CategoryType.Model)
            .OrderBy(x => x.Name)
            .ToList();
    }

    private void UpdateParameters()
    {
        Parameters = GetParameterNamesForCategory(_doc, SelectedCategory.BuiltInCategory);
    }

    private List<Category> GetAllCategoryByType(Document doc, CategoryType categoryType)
    {
        Categories categories = _doc.Settings.Categories;
        return categories.Cast<Category>()
            .Where(c => c.CategoryType == categoryType).ToList();
    }

    List<string> GetParameterNamesForCategory(Document doc, BuiltInCategory builtInCategory)
    {
        List<string> names = new List<string>();
        Category category = Category.GetCategory(doc, builtInCategory);

        DefinitionBindingMapIterator it = doc.ParameterBindings.ForwardIterator();
        while (it.MoveNext())
        {
            Definition def = it.Key;
            Binding binding = it.Current as Binding;
            names.Add(def.Name);
            if (binding is InstanceBinding instanceBinding &&
                instanceBinding.Categories.Contains(category))
            {
            }
        }

        return names;
    }

    [RelayCommand]
    private void SelectParameter(Button button)
    {
        if (button != null)
        {
         

            // Устанавливаем целевой элемент для Popup
            CurrentPopupTarget = button;

            // Открываем Popup
            IsPopupOpen = true;
        }
    }
}