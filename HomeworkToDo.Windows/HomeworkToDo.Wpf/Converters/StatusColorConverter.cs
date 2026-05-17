using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace HomeworkToDo.Wpf.Converters;

public class StatusColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool isOverdue) return DependencyProperty.UnsetValue;
        return isOverdue
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))    // Red
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 160, 30));  // Orange
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
