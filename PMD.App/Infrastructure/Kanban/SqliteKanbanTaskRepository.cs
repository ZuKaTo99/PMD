using PMD.App.Application.Kanban;
using PMD.App.Domain.Kanban;
using PMD.App.Infrastructure.Database;
using PMD.App.Infrastructure.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Infrastructure.Kanban;

public sealed class SqliteKanbanTaskRepository : IKanbanTaskRepository
{
    private readonly IPmdDatabaseConnectionFactory connectionFactory;

    public SqliteKanbanTaskRepository(
        IPmdDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public IReadOnlyList<KanbanTask> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();

        return connection
            .Table<KanbanTaskRecord>()
            .ToList()
            .OrderBy(record => record.Status)
            .ThenBy(record => record.SortOrder)
            .ThenByDescending(record => record.CreatedAt)
            .Select(MapToTask)
            .ToList();
    }

    public KanbanTask? GetById(Guid taskId)
    {
        using var connection = connectionFactory.CreateConnection();

        KanbanTaskRecord? record = connection
            .Table<KanbanTaskRecord>()
            .FirstOrDefault(task => task.Id == taskId.ToString());

        return record is null
            ? null
            : MapToTask(record);
    }

    public void Save(KanbanTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        using var connection = connectionFactory.CreateConnection();
        connection.InsertOrReplace(MapToRecord(task));
    }

    public void Delete(Guid taskId)
    {
        using var connection = connectionFactory.CreateConnection();

        connection.Execute(
            "DELETE FROM KanbanTasks WHERE Id = ?",
            taskId.ToString());
    }

    private static KanbanTask MapToTask(KanbanTaskRecord record)
    {
        Guid? projectId = Guid.TryParse(record.ProjectId, out Guid parsedProjectId)
            ? parsedProjectId
            : null;

        KanbanTaskStatus status = Enum.IsDefined(
            typeof(KanbanTaskStatus),
            record.Status)
                ? (KanbanTaskStatus)record.Status
                : KanbanTaskStatus.Open;

        KanbanTaskPriority priority = Enum.IsDefined(
            typeof(KanbanTaskPriority),
            record.Priority)
                ? (KanbanTaskPriority)record.Priority
                : KanbanTaskPriority.Normal;

        return new KanbanTask
        {
            Id = Guid.Parse(record.Id),
            Title = record.Title,
            Description = record.Description,
            ProjectId = projectId,
            Status = status,
            Priority = priority,
            SortOrder = record.SortOrder,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    private static KanbanTaskRecord MapToRecord(KanbanTask task)
    {
        return new KanbanTaskRecord
        {
            Id = task.Id.ToString(),
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId?.ToString() ?? string.Empty,
            Status = (int)task.Status,
            Priority = (int)task.Priority,
            SortOrder = task.SortOrder,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
