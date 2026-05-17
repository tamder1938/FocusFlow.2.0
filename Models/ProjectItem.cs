using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusFlow.Models;

public class ProjectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
}