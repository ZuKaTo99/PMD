using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectHistory;

public interface IProjectHistoryService
{
    IReadOnlyList<ProjectState> GetProjectStates(Guid projectId);

    ProjectHistoryDetails? GetDetails(
        Guid projectId,
        Guid projectStateId);

    ProjectHistoryComparisonDetails? GetComparisonDetails(
        Guid projectId,
        Guid olderProjectStateId,
        Guid newerProjectStateId);
}
