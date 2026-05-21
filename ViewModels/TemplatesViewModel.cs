using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Models;
using FocusFlow.Services;
using System.Collections.ObjectModel;

namespace FocusFlow.ViewModels;

public partial class TemplatesViewModel : ObservableObject
{
    private readonly TemplateService _templateService;

    public ObservableCollection<TaskTemplate> TaskTemplates { get; } = new();
    public ObservableCollection<EventTemplate> EventTemplates { get; } = new();

    [ObservableProperty]
    private TaskTemplate? selectedTaskTemplate;

    [ObservableProperty]
    private EventTemplate? selectedEventTemplate;

    public TemplatesViewModel(TemplateService templateService)
    {
        _templateService = templateService;
        Load();
    }

    [RelayCommand]
    public void Load()
    {
        TaskTemplates.Clear();
        foreach (var template in _templateService.GetTaskTemplates())
            TaskTemplates.Add(template);

        EventTemplates.Clear();
        foreach (var template in _templateService.GetEventTemplates())
            EventTemplates.Add(template);
    }

    [RelayCommand]
    public void DeleteSelectedTaskTemplate()
    {
        if (SelectedTaskTemplate == null) return;
        _templateService.DeleteTaskTemplate(SelectedTaskTemplate.Id);
        Load();
    }

    [RelayCommand]
    public void DeleteSelectedEventTemplate()
    {
        if (SelectedEventTemplate == null) return;
        _templateService.DeleteEventTemplate(SelectedEventTemplate.Id);
        Load();
    }
}
