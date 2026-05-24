using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;

namespace FocusFlow.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly IDatabaseService _db;
    private readonly ITemplateService _templateService;
    private MonthViewModel? _currentMonthVM;
    private Action<DateTime>? _daySelectedHandler;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private object? _currentCalendarView;
    [ObservableProperty] private string _currentViewTitle = "Day";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDateFormatted))]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty] private TaskListViewModel? _currentTaskListViewModel;
    [ObservableProperty] private TimerViewModel? _currentTimerViewModel;

    // Свойства для маленького виджета аналитики "План vs Факт"
    [ObservableProperty] private string _todayPlanTimeStr = "0:00";
    [ObservableProperty] private string _todayFactTimeStr = "0:00";
    [ObservableProperty] private double _todayFactProgress;
    [ObservableProperty] private string _todayDeviationStr = "0%";
    [ObservableProperty] private IBrush _todayDeviationColor = Brushes.Gray;

    public string SelectedDateFormatted
    {
        get
        {
            var culture = Loc.CurrentLanguage == "English" ? new CultureInfo("en-US") : new CultureInfo("ru-RU");
            return SelectedDate.ToString("dd.MM.yyyy", culture);
        }
    }

    public bool IsDayView => CurrentCalendarView is DayViewModel;
    public bool IsWeekView => CurrentCalendarView is WeekViewModel;
    public bool IsMonthView => CurrentCalendarView is MonthViewModel;
    public bool IsYearView => CurrentCalendarView is YearViewModel;
    public bool IsNotDayView => !IsDayView;
    public bool IsNotWeekView => !IsWeekView;
    public bool IsNotMonthView => !IsMonthView;
    public bool IsNotYearView => !IsYearView;

    public MainViewModel(IServiceProvider services, ITemplateService templateService)
    {
        _services = services;
        _templateService = templateService;
        _db = _services.GetRequiredService<IDatabaseService>();

        CurrentTaskListViewModel = _services.GetRequiredService<TaskListViewModel>();
        CurrentTimerViewModel = _services.GetRequiredService<TimerViewModel>();

        if (CurrentTaskListViewModel != null)
            CurrentTaskListViewModel.FocusRequested += OnFocusRequested;

        Loc.PropertyChanged += (s, e) =>
        {
            UpdateTitleTranslation();
            OnPropertyChanged(nameof(SelectedDateFormatted));
            RefreshTodayMiniStats();
        };

        SwitchToDay();
        RefreshTodayMiniStats();
    }

    public void RefreshTodayMiniStats()
    {
        var today = DateTime.Today;
        var tasks = _db.GetTasksByDate(today).ToList();
        int totalPlannedMinutes = tasks.Sum(t => t.PlannedDurationMinutes);

        var sessions = _db.GetSessionsForDate(today).ToList();
        int totalActualMinutes = sessions.Sum(s => s.ActualMinutes > 0 ? s.ActualMinutes : s.PlannedMinutes);

        TodayPlanTimeStr = $"{totalPlannedMinutes / 60}:{totalPlannedMinutes % 60:D2}";
        TodayFactTimeStr = $"{totalActualMinutes / 60}:{totalActualMinutes % 60:D2}";
        TodayFactProgress = totalPlannedMinutes > 0 ? Math.Min(100, ((double)totalActualMinutes / totalPlannedMinutes) * 100) : 0;

        if (totalPlannedMinutes > 0)
        {
            int diffMinutes = totalActualMinutes - totalPlannedMinutes;
            double pct = ((double)diffMinutes / totalPlannedMinutes) * 100;
            if (pct >= 0)
            {
                TodayDeviationStr = $"+{Math.Round(pct)}%";
                TodayDeviationColor = Brush.Parse("#10B981");
            }
            else
            {
                TodayDeviationStr = $"{Math.Round(pct)}%";
                TodayDeviationColor = Brush.Parse("#EF4444");
            }
        }
        else
        {
            TodayDeviationStr = "0%";
            TodayDeviationColor = Brushes.Gray;
        }
    }

    private void OnFocusRequested(TaskItem task)
    {
        CurrentTimerViewModel?.StartFocusForTask(task);
    }

    private void UpdateTitleTranslation()
    {
        if (IsDayView) CurrentViewTitle = Loc["TitleDay"];
        else if (IsWeekView) CurrentViewTitle = Loc["TitleWeek"];
        else if (IsMonthView) CurrentViewTitle = Loc["TitleMonth"];
        else if (IsYearView) CurrentViewTitle = Loc["TitleYear"];
    }

    [RelayCommand]
    private void GoToToday()
    {
        SelectedDate = DateTime.Today;
        if (CurrentCalendarView is DayViewModel dayVm) dayVm.SelectedDate = DateTime.Today;
        RefreshTodayMiniStats();
    }

    [RelayCommand]
    private void OpenAnalytics()
    {
        var analyticsVm = new AnalyticsViewModel(_db);
        var window = new AnalyticsWindow { DataContext = analyticsVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;
        if (owner != null && owner.IsVisible)
            window.ShowDialog(owner);
        else
            window.Show();
    }

    [RelayCommand]
    private void SwitchToDay()
    {
        if (_currentMonthVM != null && _daySelectedHandler != null) _currentMonthVM.DaySelected -= _daySelectedHandler;
        _currentMonthVM = null; _daySelectedHandler = null;

        var vm = _services.GetRequiredService<DayViewModel>();
        vm.SelectedDate = SelectedDate;
        vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(DayViewModel.SelectedDate)) SelectedDate = vm.SelectedDate; };
        CurrentCalendarView = vm;
        UpdateTitleTranslation(); UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToYear()
    {
        if (_currentMonthVM != null && _daySelectedHandler != null) _currentMonthVM.DaySelected -= _daySelectedHandler;
        _currentMonthVM = null; _daySelectedHandler = null;

        var vm = _services.GetRequiredService<YearViewModel>();
        vm.GoToYear(SelectedDate.Year);
        Action<DateTime> yearDayHandler = date => SwitchToDay(date);
        vm.DaySelected += yearDayHandler;

        CurrentCalendarView = vm;
        UpdateTitleTranslation(); UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToWeek()
    {
        if (_currentMonthVM != null && _daySelectedHandler != null) _currentMonthVM.DaySelected -= _daySelectedHandler;
        _currentMonthVM = null; _daySelectedHandler = null;

        var vm = _services.GetRequiredService<WeekViewModel>();
        vm.GoToWeek(SelectedDate);
        CurrentCalendarView = vm;
        UpdateTitleTranslation(); UpdateViewFlags();
    }

    [RelayCommand]
    private void SwitchToMonth()
    {
        if (_currentMonthVM != null && _daySelectedHandler != null) _currentMonthVM.DaySelected -= _daySelectedHandler;

        _currentMonthVM = _services.GetRequiredService<MonthViewModel>();
        _daySelectedHandler = date => SwitchToDay(date);
        _currentMonthVM.DaySelected += _daySelectedHandler;

        CurrentCalendarView = _currentMonthVM;
        UpdateTitleTranslation(); UpdateViewFlags();
    }

    private void SwitchToDay(DateTime date)
    {
        if (CurrentCalendarView is DayViewModel dayVm) dayVm.SelectedDate = date;
        else
        {
            if (_currentMonthVM != null && _daySelectedHandler != null) _currentMonthVM.DaySelected -= _daySelectedHandler;
            _currentMonthVM = null; _daySelectedHandler = null;

            var vm = _services.GetRequiredService<DayViewModel>();
            vm.SelectedDate = date;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(DayViewModel.SelectedDate)) SelectedDate = vm.SelectedDate; };
            CurrentCalendarView = vm;
            UpdateTitleTranslation(); UpdateViewFlags();
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsVm = new SettingsViewModel();
        var window = new SettingsWindow { DataContext = settingsVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;
        if (owner != null && owner.IsVisible)
            window.ShowDialog(owner);
        else
            window.Show();
    }

    [RelayCommand]
    private void OpenTemplates()
    {
        var templatesVm = new TemplatesViewModel(_templateService);
        var window = new TemplatesWindow { DataContext = templatesVm };
        var desktop = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;
        if (owner != null && owner.IsVisible)
            window.ShowDialog(owner);
        else
            window.Show();
    }

    private void UpdateViewFlags()
    {
        OnPropertyChanged(nameof(IsDayView));
        OnPropertyChanged(nameof(IsWeekView));
        OnPropertyChanged(nameof(IsMonthView));
        OnPropertyChanged(nameof(IsYearView));
    }
}