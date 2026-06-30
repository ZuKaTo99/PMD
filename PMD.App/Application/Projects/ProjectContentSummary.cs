using System;
using System.Collections.Generic;

namespace PMD.App.Application.Projects;

public sealed class ProjectContentSummary
{
    public static ProjectContentSummary Empty { get; } = new();

    public string ProfileLabel { get; init; } = "Noch nicht ausgewertet";

    public string ShortDescription { get; init; } = "Nach der ersten Prüfung fasst PMD den Projektinhalt zusammen.";

    public int FileCount { get; init; }

    public long TotalSizeInBytes { get; init; }

    public int ScannedFolderCount { get; init; }

    public int IgnoredFolderCount { get; init; }

    public int WarningCount { get; init; }

    public TimeSpan ScanDuration { get; init; }

    public int DifferentFileTypeCount { get; init; }

    public int CodeFileCount { get; init; }

    public int ConfigFileCount { get; init; }

    public int TextFileCount { get; init; }

    public int OtherFileCount { get; init; }

    public IReadOnlyList<ProjectFileTypeSummary> FrequentFileTypes { get; init; } = Array.Empty<ProjectFileTypeSummary>();

    public IReadOnlyList<ProjectFileHighlight> LargerFiles { get; init; } = Array.Empty<ProjectFileHighlight>();

    public bool HasData => FileCount > 0;

    public bool HasWarnings => WarningCount > 0;
}

public sealed record ProjectFileTypeSummary(
    string Extension,
    int FileCount,
    long TotalSizeInBytes);

public sealed record ProjectFileHighlight(
    string FileName,
    string RelativePath,
    string Extension,
    long SizeInBytes,
    DateTime LastChangedAt);
