using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectHistory;

public sealed class ProjectHistoryDetails
{
    public Project Project { get; init; } = default!;

    public IReadOnlyList<ProjectState> ProjectStates { get; init; } =
        Array.Empty<ProjectState>();

    public ProjectState SelectedProjectState { get; init; } = default!;

    public ProjectState? PreviousProjectState { get; init; }

    public ProjectState? NewerProjectState { get; init; }

    public ProjectState? OlderProjectState { get; init; }

    public ProjectChangesResult? ChangesFromPreviousState { get; init; }

    public int ProjectStateNumber { get; init; }

    public int TotalProjectStateCount => ProjectStates.Count;
}
