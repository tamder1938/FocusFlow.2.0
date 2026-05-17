using System;
using System.Collections.ObjectModel;

namespace FocusFlow.Models;

public class MonthDayItem
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public string DayNumber { get; set; } = string.Empty;
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();
}
