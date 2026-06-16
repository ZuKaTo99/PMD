using System;
using System.Collections.Generic;

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

    public List<string> NewFilePaths { get; init; } = new();

    public List<string> ChangedFilePaths { get; init; } = new();

    public List<string> DeletedFilePaths { get; init; } = new();
}