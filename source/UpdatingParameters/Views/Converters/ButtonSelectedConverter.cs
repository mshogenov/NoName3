using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using NoNameApi.Views.Services;
using Color = System.Windows.Media.Color;

namespace UpdatingParameters.Views.Converters;

public class ButtonSelectedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return RevitThemeManager.GetBrush(RevitThemeManager.DefaultButtonBackgroundKey);
        }

        if (value.ToString() == parameter.ToString())
        {
            return RevitThemeManager.GetBrush(RevitThemeManager.SelectedButtonBackgroundKey);
        }
        else
        {
            return RevitThemeManager.GetBrush(RevitThemeManager.DefaultButtonBackgroundKey);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}