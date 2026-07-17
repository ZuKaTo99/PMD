using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectHistory;

public sealed class ProjectHistoryComparisonDetails
{
    public Project Project { get; init; } = default!;

    public IReadOnlyList<ProjectState> ProjectStates { get; init; } =
        Array.Empty<ProjectState>();

    public ProjectState OlderProjectState { get; init; } = default!;

    public ProjectState NewerProjectState { get; init; } = default!;

    public ProjectChangesResult ChangesResult { get; init; } = default!;

    public int OlderProjectStateNumber { get; init; }

    public int NewerProjectStateNumber { get; init; }

    public int TotalProjectStateCount => ProjectStates.Count;

    public int FileCountDifference =>
        NewerProjectState.FileCount - OlderProjectState.FileCount;

    public long ProjectSizeDifferenceInBytes =>
        NewerProjectState.TotalSizeInBytes -
        OlderProjectState.TotalSizeInBytes;
}
