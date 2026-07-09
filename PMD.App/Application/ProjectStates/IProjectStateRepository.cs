using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateRepository
{
    IReadOnlyList<ProjectState> GetLatest(int maxCount);

    ProjectState? GetLatestByProjectId(Guid projectId);

    IReadOnlyList<ProjectState> GetByProjectId(Guid projectId, int maxCount);

    IReadOnlyList<ProjectStateFile> GetFilesByProjectStateId(Guid projectStateId);

    void Save(ProjectState projectState);

    void DeleteByProjectId(Guid projectId);

    void DeleteAll();
}