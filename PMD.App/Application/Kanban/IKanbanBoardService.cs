using PMD.App.Domain.Kanban;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Kanban;

public interface IKanbanBoardService
{
    event Action? BoardChanged;

    IReadOnlyList<KanbanTask> Tasks { get; }

    KanbanTask CreateTask(
        string title,
        string description,
        Guid? projectId,
        KanbanTaskStatus status,
        KanbanTaskPriority priority,
        DateTime? dueDate = null,
        string linkedFileRelativePath = "");

    KanbanTask UpdateTask(
        Guid taskId,
        string title,
        string description,
        Guid? projectId,
        KanbanTaskStatus status,
        KanbanTaskPriority priority,
        DateTime? dueDate = null,
        string linkedFileRelativePath = "");

    void DeleteTask(Guid taskId);

    void MoveTask(
        Guid taskId,
        KanbanTaskStatus targetStatus,
        int targetIndex);
}