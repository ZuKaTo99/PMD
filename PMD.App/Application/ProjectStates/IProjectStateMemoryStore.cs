using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateMemoryStore
{
    event Action? ProjectStatesChanged;

    IReadOnlyList<ProjectState> ProjectStates { get; }

    bool Remember(ProjectState projectState);

    void Clear();
}
