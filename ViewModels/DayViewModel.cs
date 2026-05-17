using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace FocusFlow.ViewModels;

public partial class DayViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private string _selectedDateFormatted = string.Empty;
    [ObservableProperty] private ObservableCollection<CalendarEvent> _events = new();
    [ObservableProperty] private ObservableCollection<EventDisplayItem> _eventDisplayItems = new();
    [ObservableProperty] private ObservableCollection<TaskItem> _dayTasks = new();
    [ObservableProperty] private ObservableCollection<string> _hourStrings = new();

    public DayViewModel(IDatabaseService db)
    {
        _db = db;

        // ИСПРАВЛЕНО: Генерация шкалы времени с ведущим нулем (00:00, 01:00... 23:00)
        for (int i = 0; i < 24; i++)
        {
            HourStrings.Add($"{i:D2}:00");
        }

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedDate))
            {
                UpdateDateFormat();
                LoadEvents();
            }
        };

        Loc.PropertyChanged += (s, e) => UpdateDateFormat();

        UpdateDateFormat();
        LoadEvents();
    }

    private void UpdateDateFormat()
    {
        var culture = Loc.CurrentLanguage == "English" ? new CultureInfo("en-US") : new CultureInfo("ru-RU");
        SelectedDateFormatted = SelectedDate.ToString("dd MMMM yyyy", culture);
    }

    public void LoadEvents()
    {
        Events.Clear();
        EventDisplayItems.Clear();
        DayTasks.Clear();

        var tasksData = _db.GetTasksByDate(SelectedDate).ToList();
        foreach (var task in tasksData)
        {
            DayTasks.Add(task);
        }

        var data = _db.GetEventsForDisplay(SelectedDate).ToList();

        foreach (var ev in data)
        {
            Events.Add(ev);

            double top = ev.Start.Hour * 60 + ev.Start.Minute;
            double height = (ev.End - ev.Start).TotalMinutes;
            if (height < 15) height = 15;

            EventDisplayItems.Add(new EventDisplayItem
            {
                Title = ev.Title,
                Color = ev.Color,
                Top = top,
                Height = height,
                Left = 4.0,
                Width = 240.0,
                OriginalEvent = ev
            });
        }
    }

    [RelayCommand]
    private async Task AddEvent()
    {
        var freshNewEvent = new CalendarEvent
        {
            Id = 0,
            Title = string.Empty,
            Start = SelectedDate.Date.AddHours(9),
            End = SelectedDate.Date.AddHours(10),
            Color = "#3498db",
            Recurrence = RecurrenceType.None
        };

        var dialogViewModel = new EventDialogViewModel(freshNewEvent, SelectedDate);
        var dialog = new EventDialog { DataContext = dialogViewModel };

        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
            if (result && dialogViewModel.ResultEvent != null)
            {
                _db.UpsertEvent(dialogViewModel.ResultEvent);
                LoadEvents();
            }
        }
    }

    public void UpdateExistingEvent(CalendarEvent ev)
    {
        _db.UpsertEvent(ev);
        LoadEvents();
    }

    public void DeleteExistingEvent(int id)
    {
        _db.DeleteEvent(id);
        LoadEvents();
    }

    [RelayCommand] private void NextDay() => SelectedDate = SelectedDate.AddDays(1);
    [RelayCommand] private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);
    [RelayCommand] private void EditTaskFromCalendar(TaskItem task) { }
}
