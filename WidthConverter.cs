using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace BobikApp;
    
public class WidthConverter : IValueConverter
{
    private const double MaxWidth = 700; // Максимальная ширина сообщения
    private const double MinWidth = 370; // Минимальная ширина сообщения
    private const double ScaleFactor = 0.8; // Коэффициент масштабированияяя


    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            double calculatedWidth = width * ScaleFactor;
            return Math.Min(Math.Max(calculatedWidth, MinWidth), MaxWidth);
        }
        return MaxWidth;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
