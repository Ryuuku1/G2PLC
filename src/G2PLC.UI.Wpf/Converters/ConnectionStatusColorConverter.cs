using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace G2PLC.UI.Wpf.Converters;

public class ConnectionStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Green
                : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
