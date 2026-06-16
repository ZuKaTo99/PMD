using System.Collections.Generic;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateMemoryStore
{
    IReadOnlyList<ProjectState> ProjectStates { get; }

    bool Remember(ProjectState projectState);

    void Clear();
}