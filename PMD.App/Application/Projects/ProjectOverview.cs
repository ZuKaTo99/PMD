using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Projects;

public sealed class ProjectOverview
{
    public Project Project { get; init; } = default!;

    public IReadOnlyList<ProjectState> ProjectStates { get; init; } = Array.Empty<ProjectState>();

    public ProjectState? LatestProjectState => ProjectStates.FirstOrDefault();

    public ProjectState? PreviousProjectState => ProjectStates.Skip(1).FirstOrDefault();

    public ProjectStateComparisonResult? ChangesSinceLastCheck { get; init; }

    public bool HasProjectHistory => ProjectStates.Count > 0;

    public bool HasEnoughChecksForChanges => ProjectStates.Count >= 2;
}
