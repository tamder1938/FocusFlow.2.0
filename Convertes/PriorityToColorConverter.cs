using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FocusFlow.Converters
{
    public class PriorityToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int priority && parameter is string paramStr && int.TryParse(paramStr, out int targetPriority))
            {
                if (priority == targetPriority)
                {
                    return targetPriority switch
                    {
                        0 => new SolidColorBrush(Color.Parse("#EF4444")), // высокий
                        1 => new SolidColorBrush(Color.Parse("#F59E0B")), // средний
                        2 => new SolidColorBrush(Color.Parse("#10B981")), // низкий
                        _ => new SolidColorBrush(Color.Parse("#6B7280"))   // нет
                    };
                }
            }
            return new SolidColorBrush(Color.Parse("#E5E7EB")); // неактивный цвет
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}