using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace BobikApp;

public class RoleToHorizontalOptionsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role == "user" ? LayoutOptions.End : LayoutOptions.Start;
        }
        return LayoutOptions.Start;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}