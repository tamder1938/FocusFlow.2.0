using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Text;

namespace FocusFlow.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Hotkey_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            e.Handled = true; // Блокируем стандартную печать букв в поле

            // Игнорируем нажатие чистых системных модификаторов
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift)
                return;

            var sb = new StringBuilder();

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) sb.Append("Ctrl+");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) sb.Append("Alt+");
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) sb.Append("Shift+");

            sb.Append(e.Key.ToString());
            textBox.Text = sb.ToString(); // Записываем комбинацию в поле
        }
    }
}
