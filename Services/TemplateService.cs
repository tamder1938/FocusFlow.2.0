using FocusFlow.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace FocusFlow.Services;

public class TemplateService
{
    private readonly LiteDatabase _database;

    public TemplateService(string databasePath)
    {
        _database = new LiteDatabase(databasePath);
    }

    public List<TaskTemplate> GetTaskTemplates() =>
        _database.GetCollection<TaskTemplate>("task_templates")
                 .FindAll()
                 .OrderBy(t => t.Name)
                 .ToList();

    public List<EventTemplate> GetEventTemplates() =>
        _database.GetCollection<EventTemplate>("event_templates")
                 .FindAll()
                 .OrderBy(t => t.Name)
                 .ToList();

    public void SaveTaskTemplate(TaskTemplate template)
    {
        _database.GetCollection<TaskTemplate>("task_templates").Upsert(template);
    }

    public void SaveEventTemplate(EventTemplate template)
    {
        _database.GetCollection<EventTemplate>("event_templates").Upsert(template);
    }

    public void DeleteTaskTemplate(int id)
    {
        _database.GetCollection<TaskTemplate>("task_templates").Delete(id);
    }

    public void DeleteEventTemplate(int id)
    {
        _database.GetCollection<EventTemplate>("event_templates").Delete(id);
    }
}
