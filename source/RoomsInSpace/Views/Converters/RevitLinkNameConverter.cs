using System.Globalization;
using System.Windows.Data;

namespace RoomsInSpaces.Views.Converters;

public class RevitLinkNameConverter:IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RevitLinkInstance linkInstance)
        {
            Element element = Context.ActiveDocument?.GetElement(linkInstance.GetTypeId());
            return element?.FindParameter(BuiltInParameter.RVT_LINK_FILE_NAME_WITHOUT_EXT)?.AsValueString() ?? "Без имени";
        }
        return "Без имени";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}