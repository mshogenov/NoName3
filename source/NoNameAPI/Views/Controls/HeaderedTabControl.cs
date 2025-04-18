using System.Windows;
using System.Windows.Controls;

namespace NoNameApi.Views.Controls;

public class HeaderedTabControl : TabControl
{
    // Статический конструктор для регистрации зависимых свойств
    static HeaderedTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedTabControl), 
            new FrameworkPropertyMetadata(typeof(HeaderedTabControl)));
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(object), typeof(HeaderedTabControl), 
            new PropertyMetadata(null));

    public object Header
    {
        get { return GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }
}