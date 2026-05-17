using LiteDB;
using System;
using System.Collections.Generic;

namespace FocusFlow.Models;

public enum RecurrenceType
{
    None,
    Daily,
    Weekdays,
    Weekly,
    Monthly,
    Yearly,
    Shift,
    Custom
}

public enum IntervalUnit
{
    Days,
    Weeks,
    Months
}

public class CalendarEvent
{
    [BsonId]
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Color { get; set; } = "#3498db";
    public int? TaskId { get; set; }

    // --- НОВЫЕ ПОЛЯ ДЛЯ ПОВТОРЕНИЙ И ОПЦИИ "ВЕСЬ ДЕНЬ" ---
    public bool IsAllDay { get; set; }
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

    // Список дней недели для еженедельного режима (хранится в базе как массив чисел)
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    // Параметры сменного графика
    public int? WorkingDays { get; set; }
    public int? OffDays { get; set; }
    public DateTime? CycleStartDate { get; set; }

    // Параметры произвольного интервала
    public int? IntervalValue { get; set; }
    public IntervalUnit? IntervalUnit { get; set; }
}
