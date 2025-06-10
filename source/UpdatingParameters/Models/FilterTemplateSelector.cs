using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.Views;

namespace UpdatingParameters.Models;

public class FilterTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item == null) return null;

        var element = container as FrameworkElement;
        if (element == null) return null;

        if (item is ViewModels.FilterGroupVM)
        {
            return new DataTemplate
            {
                VisualTree = new FrameworkElementFactory(typeof(FilterGroupControl))
            };
        }

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