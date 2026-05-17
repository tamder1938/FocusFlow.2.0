using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlow.ViewModels;

public class AnalyticsSegment
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string TimeFormatted { get; set; } = "0 ч 00 м";
    public double Percentage { get; set; }
    public string Color { get; set; } = "#3498db";
}

public class DayChartItem
{
    public string DayName { get; set; } = string.Empty;
    public double Hours { get; set; }
    public double HeightRatio { get; set; }
    public string Color { get; set; } = "#93C5FD";
}

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    [ObservableProperty] private DateTime _weekStart;
    [ObservableProperty] private ObservableCollection<AnalyticsSegment> _segments = new();
    [ObservableProperty] private ObservableCollection<DayChartItem> _weekDaysChart = new();

    // Блок 1
    [ObservableProperty] private string _totalFocusedTimeStr = "0 ч 00 м";
    [ObservableProperty] private string _weekComparisonStr = "0% за неделю";
    [ObservableProperty] private string _weekComparisonColor = "#9CA3AF";

    // Блок 2
    [ObservableProperty] private string _completedTasksRatio = "0 / 0";
    [ObservableProperty] private double _tasksProgressPercentage;

    // Блок 3
    [ObservableProperty] private double _productivityPercentage;
    [ObservableProperty] private string _productivityLevel = "Низкий";
    [ObservableProperty] private string _productivityColor = "#EF4444";

    [ObservableProperty] private double _totalHours;

    public AnalyticsViewModel(IDatabaseService db)
    {
        _db = db;
        int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
        WeekStart = DateTime.Today.AddDays(-diff);
        LoadData();
    }

    partial void OnWeekStartChanged(DateTime value) => LoadData();

    [RelayCommand] private void PreviousWeek() => WeekStart = WeekStart.AddDays(-7);
    [RelayCommand] private void NextWeek() => WeekStart = WeekStart.AddDays(7);

    [RelayCommand]
    private void CloseWindow(Avalonia.Controls.Window window) => window?.Close();

    private void LoadData()
    {
        var end = WeekStart.AddDays(7);

        var currentWeekSessions = _db.GetSessionsForPeriod(WeekStart, end).ToList();
        var currentWeekTasks = _db.GetTasksForPeriod(WeekStart, end).ToList();

        // --- КАРТОЧКА 1: ВСЕГО СФОКУСИРОВАН ---
        int totalMinutes = currentWeekSessions.Sum(s => s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes);
        TotalHours = totalMinutes / 60.0;
        TotalFocusedTimeStr = $"{totalMinutes / 60} ч {totalMinutes % 60} м";

        var prevWeekStart = WeekStart.AddDays(-7);
        var prevWeekSessions = _db.GetSessionsForPeriod(prevWeekStart, WeekStart).ToList();
        int prevTotalMinutes = prevWeekSessions.Sum(s => s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes);

        if (prevTotalMinutes > 0)
        {
            double diffPct = ((double)(totalMinutes - prevTotalMinutes) / prevTotalMinutes) * 100;
            if (diffPct >= 0)
            {
                WeekComparisonStr = $"↑ {Math.Round(diffPct)}% за неделю";
                WeekComparisonColor = "#10B981";
            }
            else
            {
                WeekComparisonStr = $"↓ {Math.Abs(Math.Round(diffPct))}% за неделю";
                WeekComparisonColor = "#EF4444";
            }
        }
        else
        {
            WeekComparisonStr = "0% за неделю";
            WeekComparisonColor = "#9CA3AF";
        }

        // --- КАРТОЧКА 2: ЗАВЕРШЕНО ЗАДАЧ ---
        int totalTasksCount = currentWeekTasks.Count;
        int completedTasksCount = currentWeekTasks.Count(t => t.IsCompleted);
        CompletedTasksRatio = $"{completedTasksCount} / {totalTasksCount}";
        TasksProgressPercentage = totalTasksCount > 0 ? ((double)completedTasksCount / totalTasksCount) * 100 : 0;

        // --- КАРТОЧКА 3: ПРОДУКТИВНОСТЬ ---
        ProductivityPercentage = totalTasksCount > 0 ? (TasksProgressPercentage * 0.6) + (Math.Min(100, (totalMinutes / 1200.0) * 100) * 0.4) : 0;
        ProductivityPercentage = Math.Min(100, Math.Round(ProductivityPercentage));

        if (ProductivityPercentage >= 75) { ProductivityLevel = "Высокий уровень"; ProductivityColor = "#10B981"; }
        else if (ProductivityPercentage >= 40) { ProductivityLevel = "Средний уровень"; ProductivityColor = "#F59E0B"; }
        else { ProductivityLevel = "Низкий уровень"; ProductivityColor = "#EF4444"; }

        // --- РАСЧЕТ ГРАФИКА: РАСПРЕДЕЛЕНИЕ ФОКУСА ПО ДНЯМ НЕДЕЛИ ---
        var daysNamesRu = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

        // ИСПРАВЛЕНО: Добавлен корректный размер массива [7]
        int[] minutesByDay = new int[7];

        for (int i = 0; i < 7; i++)
        {
            var dateToCheck = WeekStart.AddDays(i);

            // ИСПРАВЛЕНО: Запись в массив по индексу [i]
            minutesByDay[i] = currentWeekSessions
                .Where(s => s.StartTime.Date == dateToCheck.Date)
                .Sum(s => s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes);
        }

        // ИСПРАВЛЕНО: Корректный вызов метода Max() для массива
        double maxMinutes = minutesByDay.Max();
        double maxWidgetHeight = 140.0; // Максимальная высота столбика в пикселях

        WeekDaysChart.Clear();
        for (int i = 0; i < 7; i++)
        {
            // ИСПРАВЛЕНО: Чтение из массива по индексу [i]
            double hours = minutesByDay[i] / 60.0;
            double height = maxMinutes > 0 ? (minutesByDay[i] / maxMinutes) * maxWidgetHeight : 10;

            // Выделяем текущий день недели более ярким цветом
            bool isToday = WeekStart.AddDays(i).Date == DateTime.Today;

            WeekDaysChart.Add(new DayChartItem
            {
                DayName = daysNamesRu[i],
                Hours = Math.Round(hours, 1),
                HeightRatio = Math.Max(10, height), // Минимум 10px для видимости точки нуля
                Color = isToday ? "#2563EB" : "#93C5FD"
            });
        }

        // --- СЕГМЕНТЫ ПРОЕКТОВ ---
        var projectGroups = new Dictionary<string, double>();
        foreach (var s in currentWeekSessions.Where(s => s.TaskId.HasValue))
        {
            var task = _db.GetTask(s.TaskId.Value);
            string project = (task == null || string.IsNullOrWhiteSpace(task.Project)) ? "Без проекта" : task.Project;
            var actualMin = s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes;

            if (projectGroups.ContainsKey(project)) projectGroups[project] += actualMin;
            else projectGroups[project] = actualMin;
        }

        double totalHoursSum = projectGroups.Sum(kv => kv.Value) / 60.0;
        var colors = new[] { "#2563EB", "#F59E0B", "#10B981", "#8B5CF6", "#EC4899" };
        int colorIdx = 0;

        Segments.Clear();
        foreach (var kv in projectGroups.OrderByDescending(k => k.Value))
        {
            var hours = kv.Value / 60.0;
            int h = (int)hours;
            int m = (int)((hours - h) * 60);

            Segments.Add(new AnalyticsSegment
            {
                Label = kv.Key,
                Value = Math.Round(hours, 1),
                TimeFormatted = $"{h} ч {m:D2} м",
                Percentage = totalHoursSum > 0 ? (hours / totalHoursSum) * 100.0 : 0,
                Color = colors[colorIdx % colors.Length]
            });
            colorIdx++;
        }
    }
}
