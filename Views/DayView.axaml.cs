using Avalonia.Controls;
using Avalonia.Input;
using FocusFlow.Models;
using FocusFlow.ViewModels;
using System;

namespace FocusFlow.Views;

public partial class DayView : UserControl
{
    public DayView()
    {
        InitializeComponent();
    }

    private async void EventTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is EventDisplayItem displayItem && displayItem.OriginalEvent != null)
        {
            if (DataContext is DayViewModel vm)
            {
                var eventCopy = new CalendarEvent
                {
                    Id = displayItem.OriginalEvent.Id,
                    Title = displayItem.OriginalEvent.Title,
                    Start = displayItem.OriginalEvent.Start,
                    End = displayItem.OriginalEvent.End,
                    Color = displayItem.OriginalEvent.Color,
                    TaskId = displayItem.OriginalEvent.TaskId,
                    IsAllDay = displayItem.OriginalEvent.IsAllDay,
                    Recurrence = displayItem.OriginalEvent.Recurrence,
                    DaysOfWeek = displayItem.OriginalEvent.DaysOfWeek != null ? new(displayItem.OriginalEvent.DaysOfWeek) : new(),
                    WorkingDays = displayItem.OriginalEvent.WorkingDays,
                    OffDays = displayItem.OriginalEvent.OffDays,
                    CycleStartDate = displayItem.OriginalEvent.CycleStartDate,
                    IntervalValue = displayItem.OriginalEvent.IntervalValue,
                    IntervalUnit = displayItem.OriginalEvent.IntervalUnit
                };

                var dialogViewModel = new EventDialogViewModel(eventCopy, vm.SelectedDate);
                var dialog = new EventDialog { DataContext = dialogViewModel };

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
                    if (result)
                    {
                        if (dialogViewModel.IsDeleted)
                        {
                            // РЕШЕНИЕ: Удаление корневой записи стирает саму серию и все виртуальные копии на другие даты
                            vm.DeleteExistingEvent(eventCopy.Id);
                        }
                        else if (dialogViewModel.ResultEvent != null)
                        {
                            vm.UpdateExistingEvent(dialogViewModel.ResultEvent);
                        }
                    }
                }
            }
        }
    }
}
