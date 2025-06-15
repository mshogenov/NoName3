using System.Collections.ObjectModel;

namespace UpdatingParameters.Models;

public class CategoryItem
{
    public string Name { get; set; }
    public bool IsChecked { get; set; }
    public ObservableCollection<CategoryItem> Children { get; set; } = new();
    public bool IsExpanded { get; set; }

    public CategoryItem(Category category)
    {
        Name = category.Name;
        foreach (Category subCategory in category.SubCategories)
        {
            Children.Add(new CategoryItem(subCategory));
        }
    }
}