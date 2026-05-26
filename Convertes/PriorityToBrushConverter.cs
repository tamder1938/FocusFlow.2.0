using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FocusFlow.Converters;

public class PriorityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return new SolidColorBrush(Color.Parse("#9CA3AF"));

        string priorityStr = value.ToString()!.ToLower();

        return priorityStr switch
        {
            "0" or "high" => new SolidColorBrush(Color.Parse("#EF4444")),
            "1" or "medium" => new SolidColorBrush(Color.Parse("#F59E0B")),
            "2" or "low" => new SolidColorBrush(Color.Parse("#10B981")),
            "3" or "none" or "" => new SolidColorBrush(Color.Parse("#9CA3AF")),
            _ => new SolidColorBrush(Color.Parse("#9CA3AF"))
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
