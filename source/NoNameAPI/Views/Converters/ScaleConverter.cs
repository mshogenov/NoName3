using System.Globalization;
using System.Windows.Data;

namespace NoNameApi.Views.Converters;

// Конвертер для масштабирования значений в зависимости от размера
public class ScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double size && parameter is string factorString)
        {
            if (double.TryParse(factorString, NumberStyles.Any, CultureInfo.InvariantCulture, out double factor))
            {
                return size * factor;
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}