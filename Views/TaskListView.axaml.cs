using Avalonia.Controls;
using Avalonia.Input;
using FocusFlow.ViewModels;

namespace FocusFlow.Views;

public partial class TaskListView : UserControl
{
    public TaskListView()
    {
        InitializeComponent();
    }

    private void OnListBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is TaskListViewModel vm && vm.SelectedTask != null)
        {
            vm.EditTaskCommand?.Execute(vm.SelectedTask);
        }
    }
}