using System;

namespace PMD.App.Application.ProjectHistory;

public interface IProjectHistoryService
{
    ProjectHistoryDetails? GetDetails(
        Guid projectId,
        Guid projectStateId);
}
