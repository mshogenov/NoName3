using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using UpdatingParameters.Models;

namespace UpdatingParameters.Views.Converters
{
    public class MeasurementUnitFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MeasurementCategory category)
            {
                var units = Enum.GetValues(typeof(MeasurementUnit))
                                .Cast<MeasurementUnit>()
                                .Where(unit =>
                                {
                                    var field = typeof(MeasurementUnit).GetField(unit.ToString());
                                    var attr = field.GetCustomAttribute<CategoryAttribute>();
                                    return attr != null && attr.Category == category;
                                })
                                .ToList();
                return units;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
