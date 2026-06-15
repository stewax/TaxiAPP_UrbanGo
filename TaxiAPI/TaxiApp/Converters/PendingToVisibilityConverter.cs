using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaxiApp.Converters;

public class PendingToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status && status.ToLower() == "pending")
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}