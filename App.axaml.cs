using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.ViewModels;
using FocusFlow.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FocusFlow;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Считываем сохраненную конфигурацию настроек
        var settings = AppSettings.Load();

        // Настройка темы оформления
        if (settings.ThemeMode == 0)
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        else if (settings.ThemeMode == 1)
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        else
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;

        // ИСПРАВЛЕНО: Загружаем сохраненный язык из JSON настроек при старте приложения
        LocalizationService.Instance.CurrentLanguage = settings.Language;

        // 2. Настраиваем контейнер служб (Dependency Injection) для передачи во все ViewModel
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 3. Создаем главное окно приложения
            var mainWindow = new MainWindow();

            // 4. Извлекаем MainViewModel из контейнера служб и привязываем его к окну
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainViewModel;

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService, DatabaseService>();

        // Регистрируем все ViewModel как Transient (создаются при каждом запросе)
        services.AddTransient<MainViewModel>();
        services.AddTransient<DayViewModel>();
        services.AddTransient<WeekViewModel>();
        services.AddTransient<MonthViewModel>();
        services.AddTransient<YearViewModel>();
        services.AddTransient<TaskListViewModel>();
        services.AddTransient<TimerViewModel>();
        services.AddSingleton<ITemplateService, TemplateService>();
    }
}
