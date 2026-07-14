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
    private const int RecentCheckLimit = 5;

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

        Dictionary<Guid, Project> projectsById = projects
            .ToDictionary(project => project.Id);

        List<HomeProjectSummary> recentProjects = projects
            .Take(RecentProjectLimit)
            .Select(CreateProjectSummary)
            .ToList();

        List<HomeProjectCheckSummary> recentChecks =
            projectStateMemoryStore.ProjectStates
                .Where(projectState =>
                    projectsById.ContainsKey(projectState.ProjectId))
                .OrderByDescending(projectState => projectState.ScannedAt)
                .Take(RecentCheckLimit)
                .Select(projectState => CreateCheckSummary(
                    projectState,
                    projectsById[projectState.ProjectId]))
                .ToList();

        return new HomeOverview
        {
            ProjectCount = projects.Count,
            LatestCheckAt = recentChecks.Count > 0
                ? recentChecks[0].ScannedAt
                : null,
            RecentProjects = recentProjects,
            RecentChecks = recentChecks
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

    private static HomeProjectCheckSummary CreateCheckSummary(
        ProjectState projectState,
        Project project)
    {
        return new HomeProjectCheckSummary
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            AccentColor = ProjectAccentColors.Normalize(
                project.AccentColor),
            ScannedAt = projectState.ScannedAt,
            ScanDuration = projectState.ScanDuration,
            FileCount = projectState.FileCount,
            WarningCount = projectState.WarningCount
        };
    }
}
