using LiteDB;
using FocusFlow.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FocusFlow.Services;

public interface IDatabaseService
{
    // События календаря
    IEnumerable<CalendarEvent> GetEvents(DateTime date);
    void UpsertEvent(CalendarEvent ev);
    void DeleteEvent(int id);

    // Задачи
    IEnumerable<TaskItem> GetAllTasks();
    TaskItem? GetTask(int id);
    void UpsertTask(TaskItem task);
    void DeleteTask(int id);
    IEnumerable<TaskItem> GetTasksByDate(DateTime date);
    IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end); // Добавлено

    // Отображение (события + задачи)
    IEnumerable<CalendarEvent> GetEventsForDisplay(DateTime date);

    // Удаление события, связанного с задачей
    void DeleteEventForTask(int taskId);

    // Сессии фокусировки
    void AddFocusSession(FocusSession session);
    void UpdateFocusSession(FocusSession session);
    IEnumerable<FocusSession> GetSessionsForTask(int taskId);
    IEnumerable<FocusSession> GetSessionsForDate(DateTime date);
    IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end); // Добавлено
    FocusSession? GetActiveSession();

    // Шаблоны таймера
    IEnumerable<TimerTemplate> GetAllTimerTemplates();
    TimerTemplate? GetTimerTemplate(int id);
    void UpsertTimerTemplate(TimerTemplate template);
    void DeleteTimerTemplate(int id);
    // Проекты
    IEnumerable<ProjectItem> GetAllProjects();
    void UpsertProject(ProjectItem project);
    void DeleteProject(int id);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _dbPath;
    private const string EventsCollection = "events";
    private const string TasksCollection = "tasks";
    private const string SessionsCollection = "sessions";
    private const string TemplatesCollection = "timer_templates";


    public DatabaseService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusFlow");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "focusflow.db");

        using var db = new LiteDatabase(_dbPath);
        var events = db.GetCollection<CalendarEvent>(EventsCollection);
        events.EnsureIndex(e => e.Start);

        var tasks = db.GetCollection<TaskItem>(TasksCollection);
        tasks.EnsureIndex(t => t.DueDate);

        var sessions = db.GetCollection<FocusSession>(SessionsCollection);
        sessions.EnsureIndex(s => s.StartTime);

        var templates = db.GetCollection<TimerTemplate>(TemplatesCollection);
        templates.EnsureIndex(t => t.Name);
    }

    public IEnumerable<CalendarEvent> GetEvents(DateTime date)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<CalendarEvent>(EventsCollection);

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var rawEvents = col.Find(e =>
            (e.Start >= startOfDay && e.Start < endOfDay && e.Recurrence == RecurrenceType.None) ||
            (e.Recurrence != RecurrenceType.None && e.Start < endOfDay)
        ).ToList();

        var computedEvents = new List<CalendarEvent>();

        foreach (var ev in rawEvents)
        {
            if (ev.Recurrence == RecurrenceType.None)
            {
                computedEvents.Add(ev);
                continue;
            }

            bool isMatch = false;
            DateTime checkDate = date.Date;

            if (ev.Start.Date > checkDate) continue;

            switch (ev.Recurrence)
            {
                case RecurrenceType.Daily:
                    isMatch = true;
                    break;

                case RecurrenceType.Weekdays:
                    isMatch = checkDate.DayOfWeek != DayOfWeek.Saturday && checkDate.DayOfWeek != DayOfWeek.Sunday;
                    break;

                case RecurrenceType.Weekly:
                    isMatch = ev.DaysOfWeek != null && ev.DaysOfWeek.Contains(checkDate.DayOfWeek);
                    break;

                case RecurrenceType.Monthly:
                    isMatch = checkDate.Day == ev.Start.Day;
                    break;

                case RecurrenceType.Yearly:
                    isMatch = checkDate.Day == ev.Start.Day && checkDate.Month == ev.Start.Month;
                    break;

                case RecurrenceType.Shift:
                    if (ev.CycleStartDate.HasValue && checkDate >= ev.CycleStartDate.Value.Date)
                    {
                        int working = ev.WorkingDays ?? 1;
                        int off = ev.OffDays ?? 1;
                        int totalCycleDays = working + off;
                        int daysPassed = (checkDate - ev.CycleStartDate.Value.Date).Days;
                        int positionInCycle = daysPassed % totalCycleDays;
                        isMatch = positionInCycle < working;
                    }
                    break;

                case RecurrenceType.Custom:
                    if (ev.IntervalValue.HasValue && ev.IntervalValue > 0)
                    {
                        int val = ev.IntervalValue.Value;
                        if (ev.IntervalUnit == IntervalUnit.Days)
                        {
                            isMatch = (checkDate - ev.Start.Date).Days % val == 0;
                        }
                        else if (ev.IntervalUnit == IntervalUnit.Weeks)
                        {
                            int daysDiff = (checkDate - ev.Start.Date).Days;
                            isMatch = (daysDiff % (val * 7) == 0);
                        }
                        else if (ev.IntervalUnit == IntervalUnit.Months)
                        {
                            int monthsDiff = (checkDate.Year - ev.Start.Year) * 12 + checkDate.Month - ev.Start.Month;
                            isMatch = (monthsDiff % val == 0) && (checkDate.Day == ev.Start.Day);
                        }
                    }
                    break;
            }

            if (isMatch)
            {
                var virtualEvent = new CalendarEvent
                {
                    Id = ev.Id,
                    Title = ev.Title,
                    Color = ev.Color,
                    TaskId = ev.TaskId,
                    IsAllDay = ev.IsAllDay,
                    Recurrence = ev.Recurrence,
                    Start = ev.IsAllDay ? checkDate : checkDate.Add(ev.Start.TimeOfDay),
                    End = ev.IsAllDay ? checkDate.AddDays(1).AddSeconds(-1) : checkDate.Add(ev.End.TimeOfDay)
                };
                computedEvents.Add(virtualEvent);
            }
        }

        return computedEvents.OrderBy(e => e.Start).ToList();
    }

    public void UpsertEvent(CalendarEvent ev)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<CalendarEvent>(EventsCollection);
        if (ev.Id == 0) col.Insert(ev); else col.Update(ev);
    }

    public void DeleteEvent(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<CalendarEvent>(EventsCollection);
        col.Delete(id);
    }

    public IEnumerable<TaskItem> GetAllTasks()
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TaskItem>(TasksCollection);
        return col.FindAll().OrderBy(t => t.DueDate).ThenByDescending(t => t.Priority).ToList();
    }

    public TaskItem? GetTask(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TaskItem>(TasksCollection);
        return col.FindById(id);
    }

    public void UpsertTask(TaskItem task)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TaskItem>(TasksCollection);
        if (task.Id == 0) col.Insert(task); else col.Update(task);
    }

    public void DeleteTask(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TaskItem>(TasksCollection);
        col.Delete(id);
    }

    public IEnumerable<TaskItem> GetTasksByDate(DateTime date)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TaskItem>(TasksCollection);
        var startOfDay = date.Date;
        return col.Find(t => t.DueDate == startOfDay).ToList();
    }

    public IEnumerable<TaskItem> GetTasksForPeriod(DateTime start, DateTime end)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TaskItem>(TasksCollection);
        return col.Find(t => t.DueDate >= start.Date && t.DueDate < end.Date).ToList();
    }

    public IEnumerable<CalendarEvent> GetEventsForDisplay(DateTime date) => GetEvents(date);
    public void DeleteEventForTask(int taskId) { }

    public void AddFocusSession(FocusSession session)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<FocusSession>(SessionsCollection);
        col.Insert(session);
    }

    public void UpdateFocusSession(FocusSession session)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<FocusSession>(SessionsCollection);
        col.Update(session);
    }

    public IEnumerable<FocusSession> GetSessionsForTask(int taskId)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<FocusSession>(SessionsCollection);
        return col.Find(s => s.TaskId == taskId).ToList();
    }

    public IEnumerable<FocusSession> GetSessionsForDate(DateTime date)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<FocusSession>(SessionsCollection);
        var start = date.Date;
        var end = start.AddDays(1);
        return col.Find(s => s.StartTime >= start && s.StartTime < end).ToList();
    }

    public IEnumerable<FocusSession> GetSessionsForPeriod(DateTime start, DateTime end)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<FocusSession>(SessionsCollection);
        return col.Find(s => s.StartTime >= start.Date && s.StartTime < end.Date).ToList();
    }

    public FocusSession? GetActiveSession() => null;

    public IEnumerable<TimerTemplate> GetAllTimerTemplates()
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
        return col.FindAll().ToList();
    }

    public TimerTemplate? GetTimerTemplate(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
        return col.FindById(id);
    }

    public void UpsertTimerTemplate(TimerTemplate template)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
        if (template.Id == 0) col.Insert(template); else col.Update(template);
    }

    public void DeleteTimerTemplate(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<TimerTemplate>(TemplatesCollection);
        col.Delete(id);
    }
    public IEnumerable<ProjectItem> GetAllProjects()
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<ProjectItem>("projects");
        return col.FindAll().ToList();
    }

    public void UpsertProject(ProjectItem project)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<ProjectItem>("projects");
        if (project.Id == 0)
            col.Insert(project);
        else
            col.Update(project);
    }

    public void DeleteProject(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<ProjectItem>("projects");
        col.Delete(id);
    }


}
