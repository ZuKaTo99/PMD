using System;

namespace PMD.App.Domain.Kanban;

public sealed class KanbanTask
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid? ProjectId { get; init; }

    public string LinkedFileRelativePath { get; init; } = string.Empty;

    public KanbanTaskStatus Status { get; init; }

    public KanbanTaskPriority Priority { get; init; }

    public int SortOrder { get; init; }

    public DateTime? DueDate { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
