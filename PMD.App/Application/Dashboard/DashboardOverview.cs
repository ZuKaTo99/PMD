using PMD.App.Application.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Dashboard;

public sealed class DashboardOverview
{
    public int ProjectCount { get; init; }

    public DateTime? LatestCheckAt { get; init; }

    public IReadOnlyList<DashboardProjectActivity> ProjectActivities { get; init; } =
        Array.Empty<DashboardProjectActivity>();

    public IReadOnlyList<ProjectLanguageUsage> LanguageUsage { get; init; } =
        Array.Empty<ProjectLanguageUsage>();

    public int ComparableProjectCount =>
        ProjectActivities.Count(activity => activity.HasComparison);

    public int TotalAddedFileCount =>
        ProjectActivities.Sum(activity => activity.AddedFileCount);

    public int TotalModifiedFileCount =>
        ProjectActivities.Sum(activity => activity.ModifiedFileCount);

    public int TotalRemovedFileCount =>
        ProjectActivities.Sum(activity => activity.RemovedFileCount);

    public int TotalLatestChangeCount =>
        TotalAddedFileCount +
        TotalModifiedFileCount +
        TotalRemovedFileCount;

    public int MaxProjectChangeCount =>
        ProjectActivities.Count == 0
            ? 0
            : ProjectActivities.Max(activity => activity.TotalChangeCount);

    public bool HasProjects => ProjectCount > 0;

    public bool HasComparisons => ComparableProjectCount > 0;

    public bool HasLanguageUsage => LanguageUsage.Count > 0;
}

public sealed class DashboardProjectActivity
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public bool HasLatestProjectState { get; init; }

    public bool HasComparison { get; init; }

    public DateTime LatestScannedAt { get; init; }

    public DateTime? PreviousScannedAt { get; init; }

    public TimeSpan LatestScanDuration { get; init; }

    public int LatestFileCount { get; init; }

    public int PreviousFileCount { get; init; }

    public int ProjectStateCount { get; init; }

    public IReadOnlyList<int> FileCountHistory { get; init; } =
        Array.Empty<int>();

    public long LatestTotalSizeInBytes { get; init; }

    public int LatestWarningCount { get; init; }

    public int AddedFileCount { get; init; }

    public int ModifiedFileCount { get; init; }

    public int RemovedFileCount { get; init; }

    public IReadOnlyList<ProjectLanguageUsage> LanguageUsage { get; init; } =
        Array.Empty<ProjectLanguageUsage>();


    public int TotalChangeCount =>
        AddedFileCount + ModifiedFileCount + RemovedFileCount;

    public int FileCountDifference =>
        HasComparison
            ? LatestFileCount - PreviousFileCount
            : 0;
}
