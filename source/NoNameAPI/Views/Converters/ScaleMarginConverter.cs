using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoNameApi.Views.Converters;

// Конвертер для создания отступов (Margin) с масштабированием
public class ScaleMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double size && parameter is string marginFactorsString)
        {
            string[] factors = marginFactorsString.Split(';');
            if (factors.Length == 4)
            {
                if (double.TryParse(factors[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double left) &&
                    double.TryParse(factors[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double top) &&
                    double.TryParse(factors[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double right) &&
                    double.TryParse(factors[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double bottom))
                {
                    return new Thickness(size * left, size * top, size * right, size * bottom);
                }
            }
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}