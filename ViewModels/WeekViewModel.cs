using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FocusFlow.ViewModels;

public partial class WeekViewModel : ObservableObject
{
    private readonly IDatabaseService _db;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private DateTime _weekStart;
    [ObservableProperty] private string _weekRangeTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<WeekDayItem> _weekDaysCustomCollection = new();
    [ObservableProperty] private ObservableCollection<string> _hourStrings = new();

    public WeekViewModel(IDatabaseService db)
    {
        _db = db;

        // ИСПРАВЛЕНО: Шкала времени для левой колонки недели приведена к двухзначному виду 00:00
        for (int i = 0; i < 24; i++)
        {
            HourStrings.Add($"{i:D2}:00");
        }

        // Подписываемся на смену языка для мгновенной перерисовки сокращений столбцов
        Loc.PropertyChanged += (s, e) => RefreshWeek();

        GoToWeek(DateTime.Today);
    }

    public void GoToWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        WeekStart = date.AddDays(-1 * diff).Date;

        DateTime weekEnd = WeekStart.AddDays(6);
        WeekRangeTitle = $"{WeekStart:dd.MM.yyyy} - {weekEnd:dd.MM.yyyy}";

        RefreshWeek();
    }

    public void RefreshWeek()
    {
        WeekDaysCustomCollection.Clear();

        for (int i = 0; i < 7; i++)
        {
            var targetDay = WeekStart.AddDays(i);

            // Конструктор внутри WeekDayItem теперь автоматически выберет en-US или ru-RU
            var dayItem = new WeekDayItem(targetDay);
            var dayEvents = _db.GetEventsForDisplay(targetDay).ToList();

            foreach (var ev in dayEvents)
            {
                dayItem.Events.Add(ev);
            }

            WeekDaysCustomCollection.Add(dayItem);
        }
    }

    [RelayCommand] private void NextWeek() => GoToWeek(WeekStart.AddDays(7));
    [RelayCommand] private void PreviousWeek() => GoToWeek(WeekStart.AddDays(-7));
}
