using System;
using System.Linq;
using System.Collections.Generic;
using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Infrastructure.ProjectStates;

public sealed class ProjectStateMemoryStore : IProjectStateMemoryStore
{
    private const int MaxRememberedProjectStates = 5;

    private readonly List<ProjectState> projectStates = new();

    public IReadOnlyList<ProjectState> ProjectStates => projectStates;

    public bool Remember(ProjectState projectState)
    {
        ArgumentNullException.ThrowIfNull(projectState);

        bool alreadyRemembered = projectStates
            .Any(existingProjectState => existingProjectState.Id == projectState.Id);

        if (alreadyRemembered)
        {
            return false;
        }

        projectStates.Insert(0, projectState);

        if (projectStates.Count > MaxRememberedProjectStates)
        {
            projectStates.RemoveRange(
                MaxRememberedProjectStates,
                projectStates.Count - MaxRememberedProjectStates);
        }

        return true;
    }

    public void Clear()
    {
        projectStates.Clear();
    }
}