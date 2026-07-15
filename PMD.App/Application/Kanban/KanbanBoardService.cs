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
