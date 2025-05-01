using System.Globalization;
using System.Windows.Data;

namespace NoNameApi.Views.Converters;

/// Конвертер для отображения иконки в зависимости от статуса
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSet = (bool)value;
        // Используем символы из Segoe MDL2 Assets
        return isSet ? "\uE73E" : "\uE711"; // ✓ или ✗
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}