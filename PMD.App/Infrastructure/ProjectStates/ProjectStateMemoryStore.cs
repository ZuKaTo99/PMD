using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Infrastructure.ProjectStates;

public sealed class ProjectStateMemoryStore : IProjectStateMemoryStore
{
    private const int MaxRememberedProjectStates = 50;

    private readonly IProjectStateRepository projectStateRepository;
    private readonly List<ProjectState> projectStates;

    public ProjectStateMemoryStore(IProjectStateRepository projectStateRepository)
    {
        this.projectStateRepository = projectStateRepository;

        projectStates = projectStateRepository
            .GetLatest(MaxRememberedProjectStates)
            .ToList();
    }

    public event Action? ProjectStatesChanged;

    public IReadOnlyList<ProjectState> ProjectStates => projectStates;

    public ProjectState? GetLatestByProjectId(Guid projectId)
    {
        ProjectState? loadedProjectState =
            projectStateRepository.GetLatestByProjectId(projectId);

        if (loadedProjectState is not null)
        {
            MergeIntoMemory(new[] { loadedProjectState });
        }

        return loadedProjectState;
    }

    public IReadOnlyList<ProjectState> GetByProjectId(Guid projectId, int maxCount)
    {
        IReadOnlyList<ProjectState> loadedProjectStates =
            projectStateRepository.GetByProjectId(projectId, maxCount);

        MergeIntoMemory(loadedProjectStates);

        return loadedProjectStates;
    }

    public IReadOnlyList<ProjectStateFile> GetFilesByProjectStateId(Guid projectStateId)
    {
        return projectStateRepository.GetFilesByProjectStateId(projectStateId);
    }

    public bool Remember(ProjectState projectState)
    {
        ArgumentNullException.ThrowIfNull(projectState);

        bool alreadyRemembered = projectStates
            .Any(existingProjectState => existingProjectState.Id == projectState.Id);

        if (alreadyRemembered)
        {
            return false;
        }

        projectStateRepository.Save(projectState);
        projectStates.Insert(0, projectState);

        if (projectStates.Count > MaxRememberedProjectStates)
        {
            projectStates.RemoveRange(
                MaxRememberedProjectStates,
                projectStates.Count - MaxRememberedProjectStates);
        }

        ProjectStatesChanged?.Invoke();
        return true;
    }

    public void RemoveByProjectId(Guid projectId)
    {
        projectStateRepository.DeleteByProjectId(projectId);
        projectStates.RemoveAll(projectState => projectState.ProjectId == projectId);
        ProjectStatesChanged?.Invoke();
    }

    public void Clear()
    {
        projectStateRepository.DeleteAll();
        projectStates.Clear();
        ProjectStatesChanged?.Invoke();
    }

    private void MergeIntoMemory(IReadOnlyList<ProjectState> loadedProjectStates)
    {
        foreach (ProjectState loadedProjectState in loadedProjectStates)
        {
            bool alreadyRemembered = projectStates
                .Any(existingProjectState => existingProjectState.Id == loadedProjectState.Id);

            if (!alreadyRemembered)
            {
                projectStates.Add(loadedProjectState);
            }
        }

        projectStates.Sort((first, second) =>
            second.ScannedAt.CompareTo(first.ScannedAt));

        if (projectStates.Count > MaxRememberedProjectStates)
        {
            projectStates.RemoveRange(
                MaxRememberedProjectStates,
                projectStates.Count - MaxRememberedProjectStates);
        }
    }
}