using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FocusFlow.Converters;

public class PriorityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Преобразуем входящее значение в строку или число для надежности
        if (value == null) return new SolidColorBrush(Color.Parse("#9CA3AF"));

        // Проверяем как int, так и enum (приводя его к строке или числу)
        string priorityStr = value.ToString()!.ToLower();

        if (priorityStr == "0" || priorityStr == "high")
            return new SolidColorBrush(Color.Parse("#EF4444")); // Красный

        if (priorityStr == "1" || priorityStr == "medium")
            return new SolidColorBrush(Color.Parse("#F59E0B")); // Оранжевый

        if (priorityStr == "2" || priorityStr == "low")
            return new SolidColorBrush(Color.Parse("#10B981")); // Зеленый

        // Во всех остальных случаях (3, "none", "нет") возвращаем серый
        return new SolidColorBrush(Color.Parse("#9CA3AF"));
    }


    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}