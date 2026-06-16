using System;

namespace PMD.App.Domain.ProjectStates;

public sealed class ProjectStateComparisonResult
{
    public Guid OldProjectStateId { get; init; }

    public Guid NewProjectStateId { get; init;}

    public int NewFileCount { get; init; }

    public int ChangedFileCount { get; init; }

    public int DeletedFileCount { get; init; }

    public int UnchangedFileCount { get; init; }

    public int TotalChangeCount =>
        NewFileCount + ChangedFileCount + DeletedFileCount;
}