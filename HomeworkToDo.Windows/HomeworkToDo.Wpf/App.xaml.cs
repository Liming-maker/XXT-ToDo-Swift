using System.Windows;
using System.Windows.Data;
using System.Globalization;
using HomeworkToDo.Core.Services;
using HomeworkToDo.Wpf.Views;

namespace HomeworkToDo.Wpf;

public partial class App : System.Windows.Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Wire up auto-start if enabled
        var storage = new StorageService();
        var settings = storage.LoadSettings();
        SetAutoStart(settings.AutoStart);

        // Create and show main window
        var mainWindow = new MainWindow();
        if (e.Args.Length > 0 && e.Args[0] == "--background")
        {
            mainWindow.Show(); // MainWindow's code-behind will hide it for --background
        }
        else
        {
            mainWindow.Show();
        }
    }

    public static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (exePath != null)
                    key.SetValue("HomeworkToDo", $"\"{exePath}\" --background");
            }
            else
            {
                key.DeleteValue("HomeworkToDo", false);
            }
        }
        catch { /* Registry not available */ }
    }
}

// ======== Additional value converters ========

public class MinuteFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int minutes) return value?.ToString() ?? "";
        if (minutes >= 1440) return $"{minutes / 1440} 天";
        if (minutes >= 60)
        {
            var h = minutes / 60;
            var m = minutes % 60;
            return m > 0 ? $"{h} 小时 {m} 分钟" : $"{h} 小时";
        }
        return $"{minutes} 分钟";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count) return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToInverseVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count) return count > 0 ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
