using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace G2PLC.UI.Wpf.Converters;

public class ConnectionStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            var key = isConnected ? "Status.Connected" : "Status.Disconnected";
            var resource = System.Windows.Application.Current?.TryFindResource(key);
            if (resource is string text)
            {
                return text;
            }
            return isConnected ? "PLC Connected" : "PLC Disconnected";
        }

        var disconnectedResource = System.Windows.Application.Current?.TryFindResource("Status.Disconnected");
        return disconnectedResource as string ?? "PLC Disconnected";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
