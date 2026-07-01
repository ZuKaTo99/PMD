using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateMemoryStore
{
    event Action? ProjectStatesChanged;

    IReadOnlyList<ProjectState> ProjectStates { get; }

    IReadOnlyList<ProjectState> GetByProjectId(Guid projectId, int maxCount);

    bool Remember(ProjectState projectState);

    void Clear();
}