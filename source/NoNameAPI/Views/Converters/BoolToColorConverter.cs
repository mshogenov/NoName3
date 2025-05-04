using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NoNameApi.Views.Converters;

/// Конвертер для изменения цвета индикатора в зависимости от статуса
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSet = (bool)value;
        return isSet ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}