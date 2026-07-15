using PMD.App.Domain.Kanban;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Kanban;

public interface IKanbanTaskRepository
{
    IReadOnlyList<KanbanTask> GetAll();

    KanbanTask? GetById(Guid taskId);

    void Save(KanbanTask task);

    void Delete(Guid taskId);
}
