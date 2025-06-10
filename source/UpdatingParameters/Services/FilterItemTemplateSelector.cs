using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.ViewModels;
using UpdatingParameters.Views;

namespace UpdatingParameters.Services;

public class FilterItemTemplateSelector : DataTemplateSelector
{
    // Метод, который определяет, какой шаблон использовать для каждого типа элемента
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        // Если элемент пустой, возвращаем null
        if (item == null) return null;

        // Получаем FrameworkElement из контейнера
        if (container is not FrameworkElement) return null;

        return item switch
        {
            // Если элемент является группой фильтров
            FilterGroupVM => new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(FilterGroupControl)) },
            // Если элемент является правилом фильтра
            FilterRuleVM => new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(FilterRuleControl)) },
            _ => null
        };
    }
}