using PMD.App.Domain.Kanban;
using System;

namespace PMD.App.Application.Kanban;

public sealed record KanbanTaskFilterCriteria(
    string SearchText,
    Guid? ProjectId,
    bool OnlyUnassignedProject,
    KanbanTaskPriority? Priority,
    KanbanTaskStatus? Status)
{
    public static KanbanTaskFilterCriteria Empty { get; } = new(
        string.Empty,
        null,
        false,
        null,
        null);

    public bool IsActive =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        ProjectId.HasValue ||
        OnlyUnassignedProject ||
        Priority.HasValue ||
        Status.HasValue;
}
