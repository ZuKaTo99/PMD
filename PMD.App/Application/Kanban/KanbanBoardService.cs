using PMD.App.Domain.Kanban;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Kanban;

public sealed class KanbanBoardService : IKanbanBoardService
{
    private const int MaximumTitleLength = 160;
    private const int MaximumDescriptionLength = 2000;

    private readonly IKanbanTaskRepository taskRepository;
    private readonly List<KanbanTask> tasks;

    public KanbanBoardService(IKanbanTaskRepository taskRepository)
    {
        this.taskRepository = taskRepository;
        tasks = taskRepository.GetAll().ToList();
        SortTasks();
    }

    public event Action? BoardChanged;

    public IReadOnlyList<KanbanTask> Tasks => tasks;

    public KanbanTask CreateTask(
        string title,
        string description,
        Guid? projectId,
        KanbanTaskStatus status,
        KanbanTaskPriority priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        string normalizedTitle = title.Trim();
        string normalizedDescription = description?.Trim() ?? string.Empty;

        if (normalizedTitle.Length > MaximumTitleLength)
        {
            throw new ArgumentException(
                $"Der Aufgabentitel darf höchstens {MaximumTitleLength} Zeichen lang sein.",
                nameof(title));
        }

        if (normalizedDescription.Length > MaximumDescriptionLength)
        {
            throw new ArgumentException(
                $"Die Beschreibung darf höchstens {MaximumDescriptionLength} Zeichen lang sein.",
                nameof(description));
        }

        KanbanTaskStatus normalizedStatus = Enum.IsDefined(
            typeof(KanbanTaskStatus),
            status)
                ? status
                : KanbanTaskStatus.Open;

        KanbanTaskPriority normalizedPriority = Enum.IsDefined(
            typeof(KanbanTaskPriority),
            priority)
                ? priority
                : KanbanTaskPriority.Normal;

        int nextSortOrder = tasks
            .Where(task => task.Status == normalizedStatus)
            .Select(task => task.SortOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;

        DateTime now = DateTime.Now;

        var task = new KanbanTask
        {
            Id = Guid.NewGuid(),
            Title = normalizedTitle,
            Description = normalizedDescription,
            ProjectId = projectId,
            Status = normalizedStatus,
            Priority = normalizedPriority,
            SortOrder = nextSortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        taskRepository.Save(task);
        tasks.Add(task);
        SortTasks();
        BoardChanged?.Invoke();

        return task;
    }

    public void MoveTask(
        Guid taskId,
        KanbanTaskStatus targetStatus,
        int targetIndex)
    {
        if (!Enum.IsDefined(typeof(KanbanTaskStatus), targetStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetStatus),
                targetStatus,
                "Der Zielstatus ist ungültig.");
        }

        KanbanTask taskToMove = tasks.FirstOrDefault(task => task.Id == taskId)
            ?? throw new KeyNotFoundException(
                $"Die Kanban-Aufgabe {taskId} wurde nicht gefunden.");

        List<KanbanTask> sourceColumnTasks = tasks
            .Where(task => task.Status == taskToMove.Status && task.Id != taskId)
            .OrderBy(task => task.SortOrder)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();

        List<KanbanTask> targetColumnTasks = taskToMove.Status == targetStatus
            ? sourceColumnTasks
            : tasks
                .Where(task => task.Status == targetStatus)
                .OrderBy(task => task.SortOrder)
                .ThenByDescending(task => task.CreatedAt)
                .ToList();

        int normalizedTargetIndex = Math.Clamp(
            targetIndex,
            0,
            targetColumnTasks.Count);

        targetColumnTasks.Insert(
            normalizedTargetIndex,
            taskToMove);

        DateTime now = DateTime.Now;
        var updatedTasksById = new Dictionary<Guid, KanbanTask>();

        ReindexColumn(
            targetColumnTasks,
            targetStatus,
            now,
            updatedTasksById);

        if (taskToMove.Status != targetStatus)
        {
            ReindexColumn(
                sourceColumnTasks,
                taskToMove.Status,
                now,
                updatedTasksById);
        }

        if (updatedTasksById.Count == 0)
        {
            return;
        }

        IReadOnlyList<KanbanTask> updatedTasks = updatedTasksById.Values
            .ToList();

        taskRepository.SaveAll(updatedTasks);

        for (int index = 0; index < tasks.Count; index++)
        {
            if (updatedTasksById.TryGetValue(
                    tasks[index].Id,
                    out KanbanTask? updatedTask))
            {
                tasks[index] = updatedTask;
            }
        }

        SortTasks();
        BoardChanged?.Invoke();
    }

    private static void ReindexColumn(
        IReadOnlyList<KanbanTask> columnTasks,
        KanbanTaskStatus status,
        DateTime updatedAt,
        IDictionary<Guid, KanbanTask> updatedTasksById)
    {
        for (int index = 0; index < columnTasks.Count; index++)
        {
            KanbanTask task = columnTasks[index];

            if (task.Status == status && task.SortOrder == index)
            {
                continue;
            }

            updatedTasksById[task.Id] = CopyTask(
                task,
                status,
                index,
                updatedAt);
        }
    }

    private static KanbanTask CopyTask(
        KanbanTask task,
        KanbanTaskStatus status,
        int sortOrder,
        DateTime updatedAt)
    {
        return new KanbanTask
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            Status = status,
            Priority = task.Priority,
            SortOrder = sortOrder,
            CreatedAt = task.CreatedAt,
            UpdatedAt = updatedAt
        };
    }

    private void SortTasks()
    {
        tasks.Sort((first, second) =>
        {
            int statusComparison = first.Status.CompareTo(second.Status);

            if (statusComparison != 0)
            {
                return statusComparison;
            }

            int orderComparison = first.SortOrder.CompareTo(second.SortOrder);

            if (orderComparison != 0)
            {
                return orderComparison;
            }

            return second.CreatedAt.CompareTo(first.CreatedAt);
        });
    }
}
