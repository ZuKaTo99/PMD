using PMD.App.Application.Analytics;
using PMD.App.Application.ProjectChanges;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Dashboard;

public sealed class DashboardOverviewService : IDashboardOverviewService
{
    private const int ProjectStatesRequiredForComparison = 2;

    private readonly IProjectMemoryStore projectMemoryStore;
    private readonly IProjectStateMemoryStore projectStateMemoryStore;
    private readonly IProjectChangesService projectChangesService;

    public DashboardOverviewService(
        IProjectMemoryStore projectMemoryStore,
        IProjectStateMemoryStore projectStateMemoryStore,
        IProjectChangesService projectChangesService)
    {
        this.projectMemoryStore = projectMemoryStore;
        this.projectStateMemoryStore = projectStateMemoryStore;
        this.projectChangesService = projectChangesService;

        projectMemoryStore.ProjectsChanged += OnSourceDataChanged;
        projectStateMemoryStore.ProjectStatesChanged += OnSourceDataChanged;
    }

    public event Action? OverviewChanged;

    public DashboardOverview GetOverview()
    {
        List<Project> projects = projectMemoryStore.Projects
            .OrderByDescending(project => project.LastScannedAt)
            .ToList();

        List<DashboardProjectActivity> activities = projects
            .Select(CreateProjectActivity)
            .OrderByDescending(activity => activity.LatestScannedAt)
            .ThenBy(activity => activity.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        DateTime? latestCheckAt = activities
            .Where(activity => activity.HasLatestProjectState)
            .Select(activity => (DateTime?)activity.LatestScannedAt)
            .FirstOrDefault();

        return new DashboardOverview
        {
            ProjectCount = projects.Count,
            LatestCheckAt = latestCheckAt,
            ProjectActivities = activities,
            LanguageUsage = ProjectLanguageUsageAnalyzer.Combine(
                activities.Select(activity => activity.LanguageUsage))
        };
    }

    private DashboardProjectActivity CreateProjectActivity(Project project)
    {
        IReadOnlyList<ProjectState> projectStates =
            projectStateMemoryStore.GetByProjectId(
                project.Id,
                ProjectStatesRequiredForComparison);

        ProjectState? latestProjectState = projectStates
            .OrderByDescending(projectState => projectState.ScannedAt)
            .FirstOrDefault();

        if (latestProjectState is null)
        {
            return new DashboardProjectActivity
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

        IReadOnlyList<ProjectLanguageUsage> languageUsage =
            ProjectLanguageUsageAnalyzer.Analyze(
                latestProjectStateWithFiles.Files);

        ProjectState? previousProjectState = projectStates
            .Where(projectState => projectState.Id != latestProjectState.Id)
            .OrderByDescending(projectState => projectState.ScannedAt)
            .FirstOrDefault();

        if (previousProjectState is null)
        {
            return CreateActivityWithoutComparison(
                project,
                latestProjectState,
                languageUsage);
        }

        ProjectState previousProjectStateWithFiles =
            projectStateMemoryStore.LoadFiles(previousProjectState);

        ProjectChangesResult comparison = projectChangesService.Compare(
            previousProjectStateWithFiles,
            latestProjectStateWithFiles);

        return new DashboardProjectActivity
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            AccentColor = ProjectAccentColors.Normalize(
                project.AccentColor),
            HasLatestProjectState = true,
            HasComparison = true,
            LatestScannedAt = latestProjectState.ScannedAt,
            PreviousScannedAt = previousProjectState.ScannedAt,
            LatestScanDuration = latestProjectState.ScanDuration,
            LatestFileCount = latestProjectState.FileCount,
            PreviousFileCount = previousProjectState.FileCount,
            LatestTotalSizeInBytes = latestProjectState.TotalSizeInBytes,
            LatestWarningCount = latestProjectState.WarningCount,
            AddedFileCount = comparison.AddedFileCount,
            ModifiedFileCount = comparison.ModifiedFileCount,
            RemovedFileCount = comparison.RemovedFileCount,
            LanguageUsage = languageUsage
        };
    }

    private static DashboardProjectActivity CreateActivityWithoutComparison(
        Project project,
        ProjectState latestProjectState,
        IReadOnlyList<ProjectLanguageUsage> languageUsage)
    {
        return new DashboardProjectActivity
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            AccentColor = ProjectAccentColors.Normalize(
                project.AccentColor),
            HasLatestProjectState = true,
            LatestScannedAt = latestProjectState.ScannedAt,
            LatestScanDuration = latestProjectState.ScanDuration,
            LatestFileCount = latestProjectState.FileCount,
            LatestTotalSizeInBytes = latestProjectState.TotalSizeInBytes,
            LatestWarningCount = latestProjectState.WarningCount,
            LanguageUsage = languageUsage
        };
    }

    private void OnSourceDataChanged()
    {
        OverviewChanged?.Invoke();
    }
}
