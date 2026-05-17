using System;
using System.Collections.ObjectModel;
using System.Globalization;
using FocusFlow.Services;

namespace FocusFlow.Models;

public class WeekDayItem
{
    public DateTime DayDate { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();

    public WeekDayItem(DateTime date)
    {
        DayDate = date.Date;

        // ДИНАМИЧЕСКИЙ ПЕРЕВОД: Выбираем культуру форматирования на основе настроек
        var currentLang = LocalizationService.Instance.CurrentLanguage;
        var culture = currentLang == "English" ? new CultureInfo("en-US") : new CultureInfo("ru-RU");

        DayName = date.ToString("ddd", culture);
        DayNumber = date.Day.ToString();
    }
}
