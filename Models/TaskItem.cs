using System;

namespace FocusFlow.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public int? Priority { get; set; }
    public int? ProjectId { get; set; }
    public string? Color { get; set; }

    // Добавлено для совместимости с AnalyticsViewModel
    public string Project { get; set; } = "Работа";
}