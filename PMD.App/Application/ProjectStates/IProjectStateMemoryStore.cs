using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateMemoryStore
{
    event Action? ProjectStatesChanged;

    IReadOnlyList<ProjectState> ProjectStates { get; }

    ProjectState? GetLatestByProjectId(Guid projectId);

    IReadOnlyList<ProjectState> GetByProjectId(
        Guid projectId,
        int maxCount);

    IReadOnlyList<ProjectStateFile> GetFilesByProjectStateId(
        Guid projectStateId);

    ProjectState LoadFiles(ProjectState projectState);

    bool Remember(ProjectState projectState);

    void RemoveByProjectId(Guid projectId);

    void Clear();
}