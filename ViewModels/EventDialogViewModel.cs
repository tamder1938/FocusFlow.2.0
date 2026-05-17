using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlow.ViewModels;

public partial class EventDialogViewModel : ObservableObject
{
    private readonly int _originalId;
    private readonly int? _originalTaskId;
    private readonly DateTime _selectedDate;

    // ГАРАНТИЯ ИСПРАВЛЕНИЯ: Добавлено свойство локализации строк для разметки EventDialog.axaml
    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _isAllDay;
    [ObservableProperty] private int _startHour;
    [ObservableProperty] private int _startMinute;
    [ObservableProperty] private int _endHour;
    [ObservableProperty] private int _endMinute;
    [ObservableProperty] private string _color;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWeeklyFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsShiftFieldsVisible))]
    [NotifyPropertyChangedFor(nameof(IsCustomFieldsVisible))]
    private int _recurrenceIndex;

    public bool IsWeeklyFieldsVisible => RecurrenceIndex == 3;
    public bool IsShiftFieldsVisible => RecurrenceIndex == 6;
    public bool IsCustomFieldsVisible => RecurrenceIndex == 7;

    public bool IsEditMode => _originalId != 0;
    public bool IsDeleted { get; private set; } = false;

    [ObservableProperty] private bool _dayMon;
    [ObservableProperty] private bool _dayTue;
    [ObservableProperty] private bool _dayWed;
    [ObservableProperty] private bool _dayThu;
    [ObservableProperty] private bool _dayFri;
    [ObservableProperty] private bool _daySat;
    [ObservableProperty] private bool _daySun;

    [ObservableProperty] private int _workingDays = 2;
    [ObservableProperty] private int _offDays = 2;
    [ObservableProperty] private DateTimeOffset? _cycleStartDate = DateTimeOffset.Now;

    [ObservableProperty] private int _intervalValue = 1;
    [ObservableProperty] private int _intervalUnitIndex = 0;

    [ObservableProperty]
    private CalendarEvent? _resultEvent;

    public EventDialogViewModel(CalendarEvent evt, DateTime selectedDate)
    {
        _originalId = evt.Id;
        _originalTaskId = evt.TaskId;
        _selectedDate = selectedDate;

        Title = evt.Title;
        IsAllDay = evt.IsAllDay;
        StartHour = evt.Start.Hour;
        StartMinute = evt.Start.Minute;
        EndHour = evt.End.Hour;
        EndMinute = evt.End.Minute;
        Color = evt.Color ?? "#3498db";
        RecurrenceIndex = (int)evt.Recurrence;

        if (evt.DaysOfWeek != null)
        {
            DayMon = evt.DaysOfWeek.Contains(DayOfWeek.Monday);
            DayTue = evt.DaysOfWeek.Contains(DayOfWeek.Tuesday);
            DayWed = evt.DaysOfWeek.Contains(DayOfWeek.Wednesday);
            DayThu = evt.DaysOfWeek.Contains(DayOfWeek.Thursday);
            DayFri = evt.DaysOfWeek.Contains(DayOfWeek.Friday);
            DaySat = evt.DaysOfWeek.Contains(DayOfWeek.Saturday);
            DaySun = evt.DaysOfWeek.Contains(DayOfWeek.Sunday);
        }

        if (evt.WorkingDays.HasValue) WorkingDays = evt.WorkingDays.Value;
        if (evt.OffDays.HasValue) OffDays = evt.OffDays.Value;
        if (evt.CycleStartDate.HasValue) CycleStartDate = new DateTimeOffset(evt.CycleStartDate.Value);
        if (evt.IntervalValue.HasValue) IntervalValue = evt.IntervalValue.Value;
        if (evt.IntervalUnit.HasValue) IntervalUnitIndex = (int)evt.IntervalUnit.Value;
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return;

        DateTime start;
        DateTime end;

        if (IsAllDay)
        {
            start = _selectedDate.Date;
            end = _selectedDate.Date.AddDays(1).AddSeconds(-1);
        }
        else
        {
            start = _selectedDate.Date.AddHours(StartHour).AddMinutes(StartMinute);
            end = _selectedDate.Date.AddHours(EndHour).AddMinutes(EndMinute);
            if (end <= start)
                return;
        }

        ResultEvent = new CalendarEvent
        {
            Id = _originalId,
            Title = Title,
            Start = start,
            End = end,
            Color = Color,
            TaskId = _originalTaskId,
            IsAllDay = IsAllDay,
            Recurrence = (RecurrenceType)RecurrenceIndex,
            DaysOfWeek = new List<DayOfWeek>()
        };

        if (DayMon) ResultEvent.DaysOfWeek.Add(DayOfWeek.Monday);
        if (DayTue) ResultEvent.DaysOfWeek.Add(DayOfWeek.Tuesday);
        if (DayWed) ResultEvent.DaysOfWeek.Add(DayOfWeek.Wednesday);
        if (DayThu) ResultEvent.DaysOfWeek.Add(DayOfWeek.Thursday);
        if (DayFri) ResultEvent.DaysOfWeek.Add(DayOfWeek.Friday);
        if (DaySat) ResultEvent.DaysOfWeek.Add(DayOfWeek.Saturday);
        if (DaySun) ResultEvent.DaysOfWeek.Add(DayOfWeek.Sunday);

        if (RecurrenceIndex == 6)
        {
            ResultEvent.WorkingDays = WorkingDays;
            ResultEvent.OffDays = OffDays;
            ResultEvent.CycleStartDate = CycleStartDate?.DateTime ?? _selectedDate.Date;
        }

        if (RecurrenceIndex == 7)
        {
            ResultEvent.IntervalValue = IntervalValue;
            ResultEvent.IntervalUnit = (IntervalUnit)IntervalUnitIndex;
        }

        CloseWindow(true);
    }

    [RelayCommand]
    private void Delete()
    {
        IsDeleted = true;
        CloseWindow(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow(false);
    }

    private void CloseWindow(bool result)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
