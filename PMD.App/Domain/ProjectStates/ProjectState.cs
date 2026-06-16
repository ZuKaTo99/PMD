using System;
using System.Collections.Generic;

namespace PMD.App.Domain.ProjectStates;

public sealed class ProjectState
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string ProjectName { get; init; } = string.Empty;

    public string RootPath { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public DateTime ScannedAt { get; init; }

    public TimeSpan ScanDuration { get; init; }

    public int FileCount { get; init; }

    public int ScannedFolderCount { get; init; }

    public int IgnoredFolderCount { get; init; }

    public int WarningCount { get; init; }

    public long TotalSizeInBytes { get; init; }

    public List<ProjectStateFile> Files { get; init; } = new();
}