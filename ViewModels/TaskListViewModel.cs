using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.Views;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FocusFlow.ViewModels;

public partial class TaskListViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    [ObservableProperty]
    private ObservableCollection<TaskItem> _tasks = new();

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private TaskItem? _selectedTask;

    public event Action<TaskItem>? FocusRequested;
    public LocalizationService Loc => LocalizationService.Instance;

    public TaskListViewModel(IDatabaseService db)
    {
        _db = db;
        LoadTasks();
    }

    private void LoadTasks()
    {
        var allTasks = _db.GetAllTasks();
        Tasks = new ObservableCollection<TaskItem>(allTasks);
    }

    [RelayCommand]
    private void ToggleTaskCompletion(TaskItem? task)
    {
        if (task == null) return;

        task.IsCompleted = !task.IsCompleted;
        _db.UpsertTask(task);

        if (task.DueDate.HasValue)
        {
            if (task.IsCompleted)
                DeleteEventByTaskId(task.Id);
            else
                CreateOrUpdateEventForTask(task);
        }

        var index = Tasks.IndexOf(task);
        if (index >= 0)
        {
            Tasks[index] = task;
        }

        RefreshCalendarUI();
        TriggerMainStatsRefresh(); // ДОБАВЛЕНО
    }

    [RelayCommand]
    private async Task AddTask()
    {
        var newTask = new TaskItem
        {
            Title = string.Empty,
            DueDate = DateTime.Today,
            Priority = 1,
            PlannedDurationMinutes = 30
        };

        var dialogViewModel = new TaskDialogViewModel(newTask);
        var dialog = new TaskDialog { DataContext = dialogViewModel };

        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        bool? result = await dialog.ShowDialog<bool?>(desktop?.MainWindow);

        if (result == true && !string.IsNullOrWhiteSpace(newTask.Title))
        {
            _db.UpsertTask(newTask);

            if (newTask.DueDate.HasValue)
            {
                CreateOrUpdateEventForTask(newTask);
            }

            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh(); // ДОБАВЛЕНО
        }
    }

    [RelayCommand]
    private async Task EditTask(TaskItem? task)
    {
        if (task == null) return;

        var dialogViewModel = new TaskDialogViewModel(task);
        var dialog = new TaskDialog { DataContext = dialogViewModel };

        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        bool? result = await dialog.ShowDialog<bool?>(desktop?.MainWindow);

        if (result == true && !string.IsNullOrWhiteSpace(task.Title))
        {
            _db.UpsertTask(task);

            if (task.DueDate.HasValue)
            {
                CreateOrUpdateEventForTask(task);
            }
            else
            {
                DeleteEventByTaskId(task.Id);
            }

            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh(); // ДОБАВЛЕНО
        }
    }

    [RelayCommand]
    private void DeleteTask(TaskItem? task)
    {
        if (task == null) return;
        DeleteEventByTaskId(task.Id);
        _db.DeleteTask(task.Id);
        LoadTasks();
        RefreshCalendarUI();
        TriggerMainStatsRefresh(); // ДОБАВЛЕНО
    }

    [RelayCommand]
    private void StartFocus(TaskItem? task)
    {
        if (task != null)
            FocusRequested?.Invoke(task);
    }

    private void CreateOrUpdateEventForTask(TaskItem task)
    {
        _db.DeleteEventForTask(task.Id);
    }

    private void DeleteEventByTaskId(int taskId)
    {
        try
        {
            _db.DeleteEventForTask(taskId);
        }
        catch { }
    }

    private void TriggerMainStatsRefresh()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.RefreshTodayMiniStats();
            }
        }
    }

    private void RefreshCalendarUI()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                if (mainVm.CurrentCalendarView is DayViewModel dayVm)
                {
                    dayVm.LoadEvents();
                }
                else if (mainVm.CurrentCalendarView is WeekViewModel weekVm)
                {
                    var start = weekVm.WeekStart;
                }
                else if (mainVm.CurrentCalendarView is MonthViewModel monthVm)
                {
                    monthVm.GoToMonth(monthVm.CurrentMonthDate);
                }
                else if (mainVm.CurrentCalendarView is YearViewModel yearVm)
                {
                    yearVm.GoToYear(yearVm.CurrentYear);
                }
            }
        }
    }
}

public static class TaskConverters
{
    public static readonly IValueConverter BoolToStrikethrough =
        new FuncValueConverter<bool, TextDecorationCollection?>(isCompleted =>
            isCompleted ? TextDecorations.Strikethrough : null);

    public static readonly IValueConverter BoolToOpacity =
        new FuncValueConverter<bool, double>(isCompleted =>
            isCompleted ? 0.5 : 1.0);
}
