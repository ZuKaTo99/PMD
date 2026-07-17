using PMD.App.Application.ProjectChanges;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.ProjectHistory;

public sealed class ProjectHistoryService : IProjectHistoryService
{
    private readonly IProjectMemoryStore projectStore;
    private readonly IProjectStateMemoryStore projectStateStore;
    private readonly IProjectChangesService projectChangesService;

    public ProjectHistoryService(
        IProjectMemoryStore projectStore,
        IProjectStateMemoryStore projectStateStore,
        IProjectChangesService projectChangesService)
    {
        this.projectStore = projectStore;
        this.projectStateStore = projectStateStore;
        this.projectChangesService = projectChangesService;
    }

    public IReadOnlyList<ProjectState> GetProjectStates(Guid projectId)
    {
        if (projectStore.GetProjectById(projectId) is null)
        {
            return Array.Empty<ProjectState>();
        }

        return LoadOrderedProjectStates(projectId);
    }

    public ProjectHistoryDetails? GetDetails(
        Guid projectId,
        Guid projectStateId)
    {
        Project? project = projectStore.GetProjectById(projectId);

        if (project is null)
        {
            return null;
        }

        List<ProjectState> projectStates =
            LoadOrderedProjectStates(projectId);

        int selectedIndex = projectStates.FindIndex(
            projectState => projectState.Id == projectStateId);

        if (selectedIndex < 0)
        {
            return null;
        }

        ProjectState selectedProjectState =
            projectStateStore.LoadFiles(projectStates[selectedIndex]);

        ProjectState? previousProjectState = selectedIndex + 1 < projectStates.Count
            ? projectStateStore.LoadFiles(projectStates[selectedIndex + 1])
            : null;

        ProjectChangesResult? changesFromPreviousState =
            previousProjectState is null
                ? null
                : projectChangesService.Compare(
                    previousProjectState,
                    selectedProjectState);

        return new ProjectHistoryDetails
        {
            Project = project,
            ProjectStates = projectStates,
            SelectedProjectState = selectedProjectState,
            PreviousProjectState = previousProjectState,
            NewerProjectState = selectedIndex > 0
                ? projectStates[selectedIndex - 1]
                : null,
            OlderProjectState = selectedIndex + 1 < projectStates.Count
                ? projectStates[selectedIndex + 1]
                : null,
            ChangesFromPreviousState = changesFromPreviousState,
            ProjectStateNumber = projectStates.Count - selectedIndex
        };
    }

    public ProjectHistoryComparisonDetails? GetComparisonDetails(
        Guid projectId,
        Guid olderProjectStateId,
        Guid newerProjectStateId)
    {
        if (olderProjectStateId == newerProjectStateId)
        {
            return null;
        }

        Project? project = projectStore.GetProjectById(projectId);

        if (project is null)
        {
            return null;
        }

        List<ProjectState> projectStates =
            LoadOrderedProjectStates(projectId);

        int olderIndex = projectStates.FindIndex(
            projectState => projectState.Id == olderProjectStateId);

        int newerIndex = projectStates.FindIndex(
            projectState => projectState.Id == newerProjectStateId);

        if (olderIndex < 0 || newerIndex < 0 || olderIndex <= newerIndex)
        {
            return null;
        }

        ProjectState olderProjectState =
            projectStateStore.LoadFiles(projectStates[olderIndex]);

        ProjectState newerProjectState =
            projectStateStore.LoadFiles(projectStates[newerIndex]);

        ProjectChangesResult changesResult =
            projectChangesService.Compare(
                olderProjectState,
                newerProjectState);

        return new ProjectHistoryComparisonDetails
        {
            Project = project,
            ProjectStates = projectStates,
            OlderProjectState = olderProjectState,
            NewerProjectState = newerProjectState,
            ChangesResult = changesResult,
            OlderProjectStateNumber = projectStates.Count - olderIndex,
            NewerProjectStateNumber = projectStates.Count - newerIndex
        };
    }

    private List<ProjectState> LoadOrderedProjectStates(Guid projectId)
    {
        return projectStateStore
            .GetByProjectId(projectId, int.MaxValue)
            .GroupBy(projectState => projectState.Id)
            .Select(group => group.First())
            .OrderByDescending(projectState => projectState.ScannedAt)
            .ThenByDescending(projectState => projectState.CreatedAt)
            .ToList();
    }
}
