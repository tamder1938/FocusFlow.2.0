using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FocusFlow.Views;

public partial class TaskDialog : Window
{
    public TaskDialog() { InitializeComponent(); }
    private void InitializeComponent() { AvaloniaXamlLoader.Load(this); }
}
