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

        string taskIdValue = taskId.ToString();

        KanbanTaskRecord? record = connection
            .Table<KanbanTaskRecord>()
            .FirstOrDefault(task => task.Id == taskIdValue);

        return record is null
            ? null
            : MapToTask(record);
    }

    public void Save(KanbanTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        SaveAll([task]);
    }

    public void SaveAll(IReadOnlyList<KanbanTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        if (tasks.Count == 0)
        {
            return;
        }

        List<KanbanTaskRecord> records = tasks
            .Select(MapToRecord)
            .ToList();

        using var connection = connectionFactory.CreateConnection();

        connection.RunInTransaction(() =>
        {
            SaveRecords(connection, records);
        });
    }

    public void Delete(Guid taskId)
    {
        using var connection = connectionFactory.CreateConnection();

        DeleteRecord(connection, taskId);
    }

    public void DeleteAndSaveAll(
        Guid taskId,
        IReadOnlyList<KanbanTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        List<KanbanTaskRecord> records = tasks
            .Select(MapToRecord)
            .ToList();

        using var connection = connectionFactory.CreateConnection();

        connection.RunInTransaction(() =>
        {
            DeleteRecord(connection, taskId);
            SaveRecords(connection, records);
        });
    }

    private static void DeleteRecord(
        SQLite.SQLiteConnection connection,
        Guid taskId)
    {
        connection.Execute(
            "DELETE FROM KanbanTasks WHERE Id = ?",
            taskId.ToString());
    }

    private static void SaveRecords(
        SQLite.SQLiteConnection connection,
        IReadOnlyList<KanbanTaskRecord> records)
    {
        foreach (KanbanTaskRecord record in records)
        {
            connection.InsertOrReplace(record);
        }
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
            LinkedFileRelativePath =
                record.LinkedFileRelativePath ?? string.Empty,
            Status = status,
            Priority = priority,
            SortOrder = record.SortOrder,
            DueDate = record.DueDate?.Date,
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
            LinkedFileRelativePath =
                task.LinkedFileRelativePath ?? string.Empty,
            Status = (int)task.Status,
            Priority = (int)task.Priority,
            SortOrder = task.SortOrder,
            DueDate = task.DueDate?.Date,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
