using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;

namespace GPause.Services;

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool suspended)
        {
            return suspended ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.LimeGreen);
        }
        return value;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class ThemeToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Manually detect the system theme using the Windows Registry
        var themeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        var currentTheme = (int)Registry.GetValue(themeKey, "AppsUseLightTheme", 1)!;
        // Determine the system theme based on the currentTheme value
        var isLightTheme = (currentTheme == 1);
        return isLightTheme ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}