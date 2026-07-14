using System;
using System.Collections.Generic;

namespace PMD.App.Application.Home;

public sealed class HomeOverview
{
    public int ProjectCount { get; init; }

    public DateTime? LatestCheckAt { get; init; }

    public IReadOnlyList<HomeProjectSummary> RecentProjects { get; init; } =
        Array.Empty<HomeProjectSummary>();

    public IReadOnlyList<HomeProjectCheckSummary> RecentChecks { get; init; } =
        Array.Empty<HomeProjectCheckSummary>();

    public HomeProjectSummary? MostRecentProject =>
        RecentProjects.Count > 0
            ? RecentProjects[0]
            : null;
}

public sealed class HomeProjectSummary
{
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public DateTime LastScannedAt { get; init; }
}

public sealed class HomeProjectCheckSummary
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = string.Empty;

    public string AccentColor { get; init; } = string.Empty;

    public DateTime ScannedAt { get; init; }

    public TimeSpan ScanDuration { get; init; }

    public int FileCount { get; init; }

    public int WarningCount { get; init; }
}
