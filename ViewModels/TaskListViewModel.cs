using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlow.ViewModels;

public partial class TaskListViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    [ObservableProperty]
    private ObservableCollection<TaskItem> _tasks = new();

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
    private async Task EditTask(TaskItem? task)
    {
        if (task == null) return;

        var dialogViewModel = new TaskDialogViewModel(task);
        var dialog = new TaskDialog { DataContext = dialogViewModel };
        var desktop = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;

        // Обязательно проверяем наличие owner
        if (owner == null)
            throw new InvalidOperationException("MainWindow is not available.");

        bool? result = await dialog.ShowDialog<bool?>(owner);

        if (result == true)
        {
            _db.UpsertTask(task);
            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh();
        }
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
        var owner = desktop?.MainWindow;

        if (owner == null)
            throw new InvalidOperationException("MainWindow is not available.");

        bool? result = await dialog.ShowDialog<bool?>(owner);

        if (result == true && !string.IsNullOrWhiteSpace(newTask.Title))
        {
            _db.UpsertTask(newTask);
            if (newTask.DueDate.HasValue)
                CreateOrUpdateEventForTask(newTask);
            LoadTasks();
            RefreshCalendarUI();
            TriggerMainStatsRefresh();
        }
    }

    private void CreateOrUpdateEventForTask(TaskItem task) => _db.DeleteEventForTask(task.Id);
    private void DeleteEventByTaskId(int id) { try { _db.DeleteEventForTask(id); } catch { } }

    private void TriggerMainStatsRefresh()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.RefreshTodayMiniStats();
    }

    private void RefreshCalendarUI()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                if (mainVm.CurrentCalendarView is DayViewModel dayVm)
                    dayVm.LoadEvents();
                else if (mainVm.CurrentCalendarView is MonthViewModel monthVm)
                    monthVm.GoToMonth(monthVm.CurrentMonthDate);
                else if (mainVm.CurrentCalendarView is YearViewModel yearVm)
                    yearVm.GoToYear(yearVm.CurrentYear);
            }
    }
}