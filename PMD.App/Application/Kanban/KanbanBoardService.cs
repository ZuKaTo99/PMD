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
        KanbanTaskPriority priority,
        DateTime? dueDate = null)
    {
        string normalizedTitle = NormalizeTitle(title);
        string normalizedDescription = NormalizeDescription(description);
        KanbanTaskStatus normalizedStatus = NormalizeStatus(status);
        KanbanTaskPriority normalizedPriority = NormalizePriority(priority);
        DateTime? normalizedDueDate = NormalizeDueDate(dueDate);

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
            DueDate = normalizedDueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        taskRepository.Save(task);
        tasks.Add(task);
        SortTasks();
        BoardChanged?.Invoke();

        return task;
    }

    public KanbanTask UpdateTask(
        Guid taskId,
        string title,
        string description,
        Guid? projectId,
        KanbanTaskStatus status,
        KanbanTaskPriority priority,
        DateTime? dueDate = null)
    {
        KanbanTask existingTask = GetRequiredTask(taskId);
        string normalizedTitle = NormalizeTitle(title);
        string normalizedDescription = NormalizeDescription(description);
        KanbanTaskStatus normalizedStatus = NormalizeStatus(status);
        KanbanTaskPriority normalizedPriority = NormalizePriority(priority);
        DateTime? normalizedDueDate = NormalizeDueDate(dueDate);
        DateTime now = DateTime.Now;

        if (existingTask.Status == normalizedStatus)
        {
            KanbanTask updatedTask = CopyTask(
                existingTask,
                normalizedTitle,
                normalizedDescription,
                projectId,
                normalizedStatus,
                normalizedPriority,
                normalizedDueDate,
                existingTask.SortOrder,
                now);

            taskRepository.Save(updatedTask);
            ReplaceTasks([updatedTask]);
            BoardChanged?.Invoke();

            return updatedTask;
        }

        List<KanbanTask> sourceColumnTasks = tasks
            .Where(task =>
                task.Status == existingTask.Status &&
                task.Id != existingTask.Id)
            .OrderBy(task => task.SortOrder)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();

        List<KanbanTask> targetColumnTasks = tasks
            .Where(task => task.Status == normalizedStatus)
            .OrderBy(task => task.SortOrder)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();

        KanbanTask movedAndUpdatedTask = CopyTask(
            existingTask,
            normalizedTitle,
            normalizedDescription,
            projectId,
            normalizedStatus,
            normalizedPriority,
            normalizedDueDate,
            targetColumnTasks.Count,
            now);

        targetColumnTasks.Add(movedAndUpdatedTask);

        var updatedTasksById = new Dictionary<Guid, KanbanTask>
        {
            [movedAndUpdatedTask.Id] = movedAndUpdatedTask
        };

        ReindexColumn(
            sourceColumnTasks,
            existingTask.Status,
            now,
            updatedTasksById);

        ReindexColumn(
            targetColumnTasks,
            normalizedStatus,
            now,
            updatedTasksById);

        IReadOnlyList<KanbanTask> updatedTasks = updatedTasksById.Values
            .ToList();

        taskRepository.SaveAll(updatedTasks);
        ReplaceTasks(updatedTasks);
        BoardChanged?.Invoke();

        return tasks.First(task => task.Id == taskId);
    }

    public void DeleteTask(Guid taskId)
    {
        KanbanTask taskToDelete = GetRequiredTask(taskId);

        List<KanbanTask> remainingColumnTasks = tasks
            .Where(task =>
                task.Status == taskToDelete.Status &&
                task.Id != taskToDelete.Id)
            .OrderBy(task => task.SortOrder)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();

        var updatedTasksById = new Dictionary<Guid, KanbanTask>();

        ReindexColumn(
            remainingColumnTasks,
            taskToDelete.Status,
            DateTime.Now,
            updatedTasksById);

        IReadOnlyList<KanbanTask> updatedTasks = updatedTasksById.Values
            .ToList();

        taskRepository.DeleteAndSaveAll(
            taskId,
            updatedTasks);

        tasks.RemoveAll(task => task.Id == taskId);
        ReplaceTasks(updatedTasks);
        BoardChanged?.Invoke();
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

        KanbanTask taskToMove = GetRequiredTask(taskId);

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
        ReplaceTasks(updatedTasks);
        BoardChanged?.Invoke();
    }

    private KanbanTask GetRequiredTask(Guid taskId)
    {
        return tasks.FirstOrDefault(task => task.Id == taskId)
            ?? throw new KeyNotFoundException(
                $"Die Kanban-Aufgabe {taskId} wurde nicht gefunden.");
    }

    private static string NormalizeTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        string normalizedTitle = title.Trim();

        if (normalizedTitle.Length > MaximumTitleLength)
        {
            throw new ArgumentException(
                $"Der Aufgabentitel darf höchstens {MaximumTitleLength} Zeichen lang sein.",
                nameof(title));
        }

        return normalizedTitle;
    }

    private static string NormalizeDescription(string description)
    {
        string normalizedDescription = description?.Trim() ?? string.Empty;

        if (normalizedDescription.Length > MaximumDescriptionLength)
        {
            throw new ArgumentException(
                $"Die Beschreibung darf höchstens {MaximumDescriptionLength} Zeichen lang sein.",
                nameof(description));
        }

        return normalizedDescription;
    }

    private static KanbanTaskStatus NormalizeStatus(
        KanbanTaskStatus status)
    {
        return Enum.IsDefined(typeof(KanbanTaskStatus), status)
            ? status
            : KanbanTaskStatus.Open;
    }

    private static KanbanTaskPriority NormalizePriority(
        KanbanTaskPriority priority)
    {
        return Enum.IsDefined(typeof(KanbanTaskPriority), priority)
            ? priority
            : KanbanTaskPriority.Normal;
    }

    private static DateTime? NormalizeDueDate(DateTime? dueDate)
    {
        return dueDate?.Date;
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
                task.Title,
                task.Description,
                task.ProjectId,
                status,
                task.Priority,
                task.DueDate,
                index,
                updatedAt);
        }
    }

    private static KanbanTask CopyTask(
        KanbanTask task,
        string title,
        string description,
        Guid? projectId,
        KanbanTaskStatus status,
        KanbanTaskPriority priority,
        DateTime? dueDate,
        int sortOrder,
        DateTime updatedAt)
    {
        return new KanbanTask
        {
            Id = task.Id,
            Title = title,
            Description = description,
            ProjectId = projectId,
            Status = status,
            Priority = priority,
            SortOrder = sortOrder,
            DueDate = dueDate,
            CreatedAt = task.CreatedAt,
            UpdatedAt = updatedAt
        };
    }

    private void ReplaceTasks(
        IReadOnlyList<KanbanTask> updatedTasks)
    {
        if (updatedTasks.Count == 0)
        {
            SortTasks();
            return;
        }

        Dictionary<Guid, KanbanTask> updatedTasksById = updatedTasks
            .ToDictionary(task => task.Id);

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
