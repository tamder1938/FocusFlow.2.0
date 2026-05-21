using LiteDB;
using System;

namespace FocusFlow.Models;

public class EventTemplate
{
    [BsonId]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = "#3498db";
    public bool IsAllDay { get; set; }
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int EndHour { get; set; } = 1;
    public int EndMinute { get; set; }
    public RecurrenceType Recurrence { get; set; }
}
