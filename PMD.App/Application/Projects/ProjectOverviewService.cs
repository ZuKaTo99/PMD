using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Projects;

public sealed class ProjectOverviewService : IProjectOverviewService
{
    private readonly IProjectMemoryStore projectStore;
    private readonly IProjectStateMemoryStore projectStateStore;

    public ProjectOverviewService(
        IProjectMemoryStore projectStore,
        IProjectStateMemoryStore projectStateStore)
    {
        this.projectStore = projectStore;
        this.projectStateStore = projectStateStore;
    }

    public ProjectOverview? GetProjectOverview(Guid projectId)
    {
        Project? project = projectStore.GetProjectById(projectId);

        if (project is null)
        {
            return null;
        }

        IReadOnlyList<ProjectState> projectStates = GetProjectStates(project);
        ProjectState? latestProjectState = projectStates.FirstOrDefault();
        ProjectState? previousProjectState = projectStates.Skip(1).FirstOrDefault();

        ProjectStateComparisonResult? changesSinceLastCheck = null;

        if (latestProjectState is not null && previousProjectState is not null)
        {
            changesSinceLastCheck = ProjectStateComparer.Compare(
                previousProjectState,
                latestProjectState);
        }

        return new ProjectOverview
        {
            Project = project,
            ProjectStates = projectStates,
            ChangesSinceLastCheck = changesSinceLastCheck
        };
    }

    private IReadOnlyList<ProjectState> GetProjectStates(Project project)
    {
        return projectStateStore.ProjectStates
            .Where(projectState => BelongsToProject(projectState, project))
            .GroupBy(projectState => projectState.Id)
            .Select(group => group.First())
            .OrderByDescending(projectState => projectState.ScannedAt)
            .ThenByDescending(projectState => projectState.CreatedAt)
            .ToList();
    }

    private static bool BelongsToProject(
        ProjectState projectState,
        Project project)
    {
        if (projectState.ProjectId == project.Id)
        {
            return true;
        }

        return ProjectStateFolderMatcher.IsSameProjectFolder(
            projectState,
            project.RootPath);
    }
}
