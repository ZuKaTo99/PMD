using PMD.App.Application.Analytics;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Home;

public sealed class HomeOverviewService : IHomeOverviewService
{
    private const int RecentProjectLimit = 4;
    private const int ActivityHistoryLimit = 7;

    private readonly IProjectMemoryStore projectMemoryStore;
    private readonly IProjectStateMemoryStore projectStateMemoryStore;

    public HomeOverviewService(
        IProjectMemoryStore projectMemoryStore,
        IProjectStateMemoryStore projectStateMemoryStore)
    {
        this.projectMemoryStore = projectMemoryStore;
        this.projectStateMemoryStore = projectStateMemoryStore;

        projectMemoryStore.ProjectsChanged += OnSourceDataChanged;
        projectStateMemoryStore.ProjectStatesChanged += OnSourceDataChanged;
    }

    public event Action? OverviewChanged;

    public HomeOverview GetOverview()
    {
        List<Project> projects = projectMemoryStore.Projects
            .OrderByDescending(project => project.LastScannedAt)
            .ToList();

        List<HomeProjectSummary> recentProjects = projects
            .Take(RecentProjectLimit)
            .Select(CreateProjectSummary)
            .ToList();

        List<HomeProjectActivitySummary> allProjectActivities = projects
            .Select(CreateProjectActivitySummary)
            .ToList();

        List<HomeProjectActivitySummary> recentProjectActivities =
            allProjectActivities
                .Take(RecentProjectLimit)
                .ToList();

        DateTime? latestCheckAt = allProjectActivities
            .Select(activity => (DateTime?)activity.LatestScannedAt)
            .FirstOrDefault();

        return new HomeOverview
        {
            ProjectCount = projects.Count,
            LatestCheckAt = latestCheckAt,
            RecentProjects = recentProjects,
            ProjectActivities = recentProjectActivities,
            LanguageUsage = ProjectLanguageUsageAnalyzer.Combine(
                allProjectActivities.Select(
                    activity => activity.LanguageUsage))
        };
    }

    private HomeProjectActivitySummary CreateProjectActivitySummary(
        Project project)
    {
        IReadOnlyList<ProjectState> projectStates =
            projectStateMemoryStore.GetByProjectId(
                project.Id,
                ActivityHistoryLimit);

        List<ProjectState> orderedProjectStates = projectStates
            .OrderBy(projectState => projectState.ScannedAt)
            .ToList();

        ProjectState? latestProjectState = orderedProjectStates
            .LastOrDefault();

        if (latestProjectState is null)
        {
            return new HomeProjectActivitySummary
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                AccentColor = ProjectAccentColors.Normalize(
                    project.AccentColor),
                LatestScannedAt = project.LastScannedAt
            };
        }

        ProjectState latestProjectStateWithFiles =
            projectStateMemoryStore.LoadFiles(latestProjectState);

        return new HomeProjectActivitySummary
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            AccentColor = ProjectAccentColors.Normalize(
                project.AccentColor),
            LatestScannedAt = latestProjectState.ScannedAt,
            LatestFileCount = latestProjectState.FileCount,
            ProjectStateCount = orderedProjectStates.Count,
            FileCountHistory = orderedProjectStates
                .Select(projectState => projectState.FileCount)
                .ToList(),
            LanguageUsage = ProjectLanguageUsageAnalyzer.Analyze(
                latestProjectStateWithFiles.Files)
        };
    }

    private void OnSourceDataChanged()
    {
        OverviewChanged?.Invoke();
    }

    private static HomeProjectSummary CreateProjectSummary(
        Project project)
    {
        return new HomeProjectSummary
        {
            ProjectId = project.Id,
            Name = project.Name,
            AccentColor = ProjectAccentColors.Normalize(
                project.AccentColor),
            LastScannedAt = project.LastScannedAt
        };
    }
}
