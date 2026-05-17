using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using System;
using System.Linq;

namespace FocusFlow.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty] private bool _isGeneralTab = true;
    [ObservableProperty] private bool _isNotificationsTab;
    [ObservableProperty] private bool _isHotkeysTab;
    [ObservableProperty] private bool _isDataTab;

    [ObservableProperty] private bool _optLightTheme;
    [ObservableProperty] private bool _optDarkTheme;
    [ObservableProperty] private bool _optAutoTheme;

    // Числовой индекс выбранного языка для стабильной связи с ComboBox
    [ObservableProperty] private int _selectedLanguageIndex;

    [ObservableProperty] private bool _systemNotifications;
    [ObservableProperty] private bool _soundNotifications;

    [ObservableProperty] private string _hotkeyDay;
    [ObservableProperty] private string _hotkeyWeek;
    [ObservableProperty] private string _hotkeyNewTask;

    public SettingsViewModel()
    {
        _settings = AppSettings.Load();

        OptLightTheme = _settings.ThemeMode == 0;
        OptDarkTheme = _settings.ThemeMode == 1;
        OptAutoTheme = _settings.ThemeMode == 2;

        // Восстанавливаем сохраненный индекс из настроек, НЕ переключая сам язык
        SelectedLanguageIndex = _settings.Language == "English" ? 1 : 0;

        SystemNotifications = _settings.SystemNotifications;
        SoundNotifications = _settings.SoundNotifications;

        HotkeyDay = _settings.HotkeyDay;
        HotkeyWeek = _settings.HotkeyWeek;
        HotkeyNewTask = _settings.HotkeyNewTask;

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IsGeneralTab) && IsGeneralTab) IsNotificationsTab = IsHotkeysTab = IsDataTab = false;
            else if (e.PropertyName == nameof(IsNotificationsTab) && IsNotificationsTab) IsGeneralTab = IsHotkeysTab = IsDataTab = false;
            else if (e.PropertyName == nameof(IsHotkeysTab) && IsHotkeysTab) IsGeneralTab = IsNotificationsTab = IsDataTab = false;
            else if (e.PropertyName == nameof(IsDataTab) && IsDataTab) IsGeneralTab = IsNotificationsTab = IsHotkeysTab = false;

            if (e.PropertyName == nameof(OptLightTheme) && OptLightTheme) OptDarkTheme = OptAutoTheme = false;
            else if (e.PropertyName == nameof(OptDarkTheme) && OptDarkTheme) OptLightTheme = OptAutoTheme = false;
            else if (e.PropertyName == nameof(OptAutoTheme) && OptAutoTheme) OptLightTheme = OptDarkTheme = false;

            // ИСПРАВЛЕНО: Мгновенное переключение языка отсюда полностью удалено!
        };
    }

    [RelayCommand]
    private void Save()
    {
        if (OptLightTheme) _settings.ThemeMode = 0;
        else if (OptDarkTheme) _settings.ThemeMode = 1;
        else if (OptAutoTheme) _settings.ThemeMode = 2;

        // 1. Конвертируем индекс и сохраняем новое значение в файл конфигурации JSON
        _settings.Language = SelectedLanguageIndex == 1 ? "English" : "Русский";
        _settings.SystemNotifications = SystemNotifications;
        _settings.SoundNotifications = SoundNotifications;

        _settings.HotkeyDay = HotkeyDay;
        _settings.HotkeyWeek = HotkeyWeek;
        _settings.HotkeyNewTask = HotkeyNewTask;

        _settings.Save();

        // 2. ПРИМЕНЯЕМ ПЕРЕВОД: Теперь язык UI обновится только в момент нажатия на кнопку "Сохранить"
        Loc.CurrentLanguage = _settings.Language;

        var app = Avalonia.Application.Current;
        if (app != null)
        {
            if (_settings.ThemeMode == 0) app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            else if (_settings.ThemeMode == 1) app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            else app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
        }

        CloseWindow();
    }

    [RelayCommand] private void Cancel() => CloseWindow();
    [RelayCommand] private async System.Threading.Tasks.Task ExportData() { }
    [RelayCommand] private void ClearData() { }

    private void CloseWindow()
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}
