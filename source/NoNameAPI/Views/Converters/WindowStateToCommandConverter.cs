using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NoNameApi.Views.Converters;

public class WindowStateToCommandConverter : IValueConverter
{
    // Свойства для настройки конвертера
    public WindowState CompareState { get; set; } = WindowState.Normal;
    public Visibility VisibilityWhenEqual { get; set; } = Visibility.Visible;
    public Visibility VisibilityWhenNotEqual { get; set; } = Visibility.Collapsed;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WindowState windowState)
        {
            return windowState == CompareState ? VisibilityWhenEqual : VisibilityWhenNotEqual;
        }

        return VisibilityWhenNotEqual;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}