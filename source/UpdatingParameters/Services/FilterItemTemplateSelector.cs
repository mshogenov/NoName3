using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.Models;
using UpdatingParameters.ViewModels;
using UpdatingParameters.Views;
using FilterRule = Autodesk.Revit.DB.FilterRule;

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
            FilterGroup => new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(FilterGroupControl)) },
            // Если элемент является правилом фильтра
            FilterRule => new DataTemplate { VisualTree = new FrameworkElementFactory(typeof(FilterRuleControl)) },
            _ => null
        };
    }
}