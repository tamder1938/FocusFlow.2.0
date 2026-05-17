using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlow.ViewModels;

public partial class TaskDialogViewModel : ObservableObject
{
    private readonly TaskItem _task;
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTimeOffset _dueDate = DateTimeOffset.Now;

    [ObservableProperty] private bool _hasDate;
    [ObservableProperty] private bool _isTimeBound;
    [ObservableProperty] private bool _isDurationSet;

    [ObservableProperty] private int _startHour = 9;
    [ObservableProperty] private int _startMinute = 0;
    [ObservableProperty] private int _durationMinutes = 30;

    // Свойства приоритета переведены на Nullable типы (null = Без сложности/Без приоритета)
    [ObservableProperty] private int? _priority = null;
    [ObservableProperty] private int _priorityIndex = -1; // -1 означает, что ничего не выбрано

    // ИСПРАВЛЕНО: Использование полного пути глобального пространства имен решает баг генератора MVVM
    [ObservableProperty] private ObservableCollection<global::FocusFlow.Models.ProjectItem> _projectsList = new();
    [ObservableProperty] private global::FocusFlow.Models.ProjectItem? _selectedProject;

    public TaskDialogViewModel(TaskItem task)
    {
        _task = task;

        // Извлекаем DatabaseService из DI контейнера служб приложения
        _db = ((App)Avalonia.Application.Current!).Services!.GetRequiredService<IDatabaseService>();

        Title = task.Title;
        Description = task.Description ?? string.Empty;

        // Восстановление приоритета задачи
        Priority = task.Priority;
        PriorityIndex = Priority switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            _ => -1 // Без сложности
        };

        // Инициализация списка проектов из LiteDB
        LoadProjectsData(task.ProjectId);

        HasDate = task.DueDate.HasValue;
        if (task.DueDate.HasValue)
        {
            var localDate = task.DueDate.Value;
            DueDate = new DateTimeOffset(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeOffset.Now.Offset);
        }
        else
        {
            DueDate = DateTimeOffset.Now;
        }

        IsTimeBound = task.StartTime.HasValue;
        if (task.StartTime.HasValue)
        {
            StartHour = task.StartTime.Value.Hours;
            StartMinute = task.StartTime.Value.Minutes;
        }

        IsDurationSet = task.PlannedDurationMinutes > 0;
        DurationMinutes = IsDurationSet ? task.PlannedDurationMinutes : 30;
    }

    private void LoadProjectsData(int? activeProjectId)
    {
        ProjectsList.Clear();

        // 1. Добавляем виртуальный дефолтный элемент "Без проекта" на нулевую позицию
        ProjectsList.Add(new ProjectItem { Id = 0, Name = "Без проекта", Color = "#9CA3AF" });

        // 2. Считываем пользовательские проекты из базы данных
        var userProjects = _db.GetAllProjects();
        foreach (var p in userProjects)
        {
            ProjectsList.Add(p);
        }

        // 3. Выставляем текущее выделение в ComboBox
        if (activeProjectId.HasValue && activeProjectId.Value > 0)
        {
            SelectedProject = ProjectsList.FirstOrDefault(p => p.Id == activeProjectId.Value) ?? ProjectsList[0];
        }
        else
        {
            SelectedProject = ProjectsList[0]; // Выделяем "Без проекта" по умолчанию
        }
    }

    [RelayCommand]
    private void SetPriority(string priorityStr)
    {
        if (int.TryParse(priorityStr, out int p))
        {
            // Если повторно нажать на уже выбранный приоритет — сбрасываем его в "Без сложности"
            if (Priority == p)
            {
                Priority = null;
                PriorityIndex = -1;
            }
            else
            {
                Priority = p;
                PriorityIndex = p;
            }
        }
    }

    [RelayCommand]
    private void ClearPriority()
    {
        Priority = null;
        PriorityIndex = -1; // Принудительный сброс в состояние "Без сложности"
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return;

        _task.Title = Title.Trim();
        _task.Description = Description.Trim();

        // Сохраняем приоритет (может быть null)
        _task.Priority = PriorityIndex >= 0 ? PriorityIndex : null;

        // Сохраняем ID проекта. Если выбран "Без проекта" (Id = 0), пишем null
        _task.ProjectId = (SelectedProject != null && SelectedProject.Id > 0) ? SelectedProject.Id : null;

        _task.DueDate = HasDate ? DueDate.LocalDateTime.Date : null;
        _task.StartTime = IsTimeBound ? new TimeSpan(StartHour, StartMinute, 0) : null;
        _task.PlannedDurationMinutes = IsDurationSet ? DurationMinutes : 0;

        // Задаем цвет подложки на основе сложности для календаря
        _task.Color = _task.Priority switch
        {
            0 => "#EF4444", // Высокий
            1 => "#F59E0B", // Средний
            2 => "#10B981", // Низкий
            _ => "#9CA3AF"  // Без сложности (серый)
        };

        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private void OpenProjectsManagement()
    {
        var managementVm = new ProjectsManagementViewModel();
        var window = new FocusFlow.Views.ProjectsManagementWindow { DataContext = managementVm };

        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Открываем окно как модальный диалог поверх текущего окна
            var currentWindow = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            if (currentWindow != null)
            {
                window.ShowDialog(currentWindow).ContinueWith(_ =>
                {
                    // После закрытия окна управления проектами обновляем список в ComboBox на лету!
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        int? currentSelectedId = SelectedProject?.Id;
                        LoadProjectsData(currentSelectedId);
                    });
                });
            }
        }
    }
}
