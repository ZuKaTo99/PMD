using PMD.App.Application.Analytics;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Home;

public sealed class HomeOverview
{
    public int ProjectCount { get; init; }

    public DateTime? LatestCheckAt { get; init; }

    public IReadOnlyList<HomeProjectSummary> RecentProjects { get; init; } =
        Array.Empty<HomeProjectSummary>();

    public IReadOnlyList<HomeProjectActivitySummary> ProjectActivities
    {
        get;
        init;
    } = Array.Empty<HomeProjectActivitySummary>();

    public IReadOnlyList<ProjectLanguageUsage> LanguageUsage { get; init; } =
        Array.Empty<ProjectLanguageUsage>();

    public HomeProjectSummary? MostRecentProject =>
        RecentProjects.Count > 0
            ? RecentProjects[0]
            : null;

    public bool HasLanguageUsage =>
        LanguageUsage.Count > 0;
}

public sealed class HomeProjectSummary
{
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public DateTime LastScannedAt { get; init; }
}

public sealed class HomeProjectActivitySummary
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public DateTime LatestScannedAt { get; init; }

    public int LatestFileCount { get; init; }

    public int ProjectStateCount { get; init; }

    public IReadOnlyList<int> FileCountHistory { get; init; } =
        Array.Empty<int>();

    public IReadOnlyList<ProjectLanguageUsage> LanguageUsage { get; init; } =
        Array.Empty<ProjectLanguageUsage>();

}
