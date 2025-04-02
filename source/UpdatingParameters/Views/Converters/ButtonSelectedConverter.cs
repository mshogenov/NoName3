using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UpdatingParameters.Views.Converters
{
    public class ButtonSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return Brushes.White;
            }

            if (value.ToString() == parameter.ToString())
            {
                return Brushes.LightBlue;
            }
            else
            {
                return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}