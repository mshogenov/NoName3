using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.Views;

namespace UpdatingParameters.Models;

public class FilterItemTemplateSelector : DataTemplateSelector
{
    // Метод, который определяет, какой шаблон использовать для каждого типа элемента
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        // Если элемент пустой, возвращаем null
        if (item == null) return null;

        // Получаем FrameworkElement из контейнера
        var element = container as FrameworkElement;
        if (element == null) return null;

        // Если элемент является группой фильтров
        if (item is ViewModels.FilterGroupVM)
        {
            return new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(FilterGroupControl))
            };
        }

        // Если элемент является правилом фильтра
        if (item is ViewModels.FilterRuleVM)
        {
            return new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(FilterRuleControl))
            };
        }

        return null;
    }
}