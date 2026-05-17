using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FocusFlow.Views;

public partial class TimerView : UserControl
{
    public TimerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
