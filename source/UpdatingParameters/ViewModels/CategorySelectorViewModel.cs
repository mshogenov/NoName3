using System.Collections.ObjectModel;
using UpdatingParameters.Models;

namespace UpdatingParameters.ViewModels;

public partial class CategorySelectorViewModel : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    public List<CategoryItem> Categories { get; } = new();

    public CategorySelectorViewModel()
    {
        InitializeCategories();
    }

    [RelayCommand]
    private void SelectAll()
    {
        SetAllChecked(Categories, true);
    }

    [RelayCommand]
    private void UnselectAll()
    {
        SetAllChecked(Categories, false);
    }

    private void SetAllChecked(IEnumerable<CategoryItem> items, bool isChecked)
    {
        foreach (var item in items)
        {
            item.IsChecked = isChecked;
            if (item.Children.Any())
            {
                SetAllChecked(item.Children, isChecked);
            }
        }
    }

    private void InitializeCategories()
    {
        var categories = GetAllCategoryByType(_doc, CategoryType.Model).OrderBy(x => x.Name);
        foreach (var category in categories)
        {
            Categories.Add(new CategoryItem(category));
        }
    }

    private List<Category> GetAllCategoryByType(Document doc, CategoryType categoryType)
    {
        Categories categories = _doc.Settings.Categories;
        return categories.Cast<Category>()
            .Where(c => c.CategoryType == categoryType).ToList();
    }
}