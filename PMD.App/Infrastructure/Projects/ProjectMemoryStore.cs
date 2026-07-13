using PMD.App.Application.Projects;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PMD.App.Application.ProjectStates;

namespace PMD.App.Infrastructure.Projects;

public sealed class ProjectMemoryStore : IProjectMemoryStore
{
    private readonly IProjectRepository projectRepository;
    private readonly IProjectStateMemoryStore projectStateMemoryStore;
    private readonly List<Project> projects;

    public ProjectMemoryStore(
        IProjectRepository projectRepository,
        IProjectStateMemoryStore projectStateMemoryStore)
    {
        this.projectRepository = projectRepository;
        this.projectStateMemoryStore = projectStateMemoryStore;
        projects = projectRepository.GetAll().ToList();
    }

    public event Action? ProjectsChanged;

    public IReadOnlyList<Project> Projects => projects;

    public Project RememberScannedProject(
        string projectName,
        string rootPath,
        DateTime scannedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        string normalizedRootPath = NormalizeRootPath(rootPath);

        Project? existingProject = GetProjectByRootPath(normalizedRootPath);

        if (existingProject is not null)
        {
            var updatedProject = new Project
            {
                Id = existingProject.Id,
                Name = existingProject.Name,
                RootPath = existingProject.RootPath,
                AccentColor = existingProject.AccentColor,
                CreatedAt = existingProject.CreatedAt,
                LastScannedAt = scannedAt
            };

            int existingIndex = projects.FindIndex(project => project.Id == existingProject.Id);

            projectRepository.Save(updatedProject);

            if (existingIndex >= 0)
            {
                projects[existingIndex] = updatedProject;
                SortProjectsByLastScannedAt();
            }

            ProjectsChanged?.Invoke();

            return updatedProject;
        }

        var newProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = projectName.Trim(),
            RootPath = normalizedRootPath,
            AccentColor = ProjectAccentColors.Default,
            CreatedAt = DateTime.Now,
            LastScannedAt = scannedAt
        };

        projectRepository.Save(newProject);
        projects.Add(newProject);
        SortProjectsByLastScannedAt();
        ProjectsChanged?.Invoke();

        return newProject;
    }

    public Project? GetProjectById(Guid projectId)
    {
        return projects.FirstOrDefault(project => project.Id == projectId);
    }

    public Project? GetProjectByRootPath(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return null;
        }

        string normalizedRootPath = NormalizeRootPath(rootPath);

        return projects.FirstOrDefault(project =>
            string.Equals(
                NormalizeRootPath(project.RootPath),
                normalizedRootPath,
                StringComparison.OrdinalIgnoreCase));
    }

    public bool UpdateProjectDetails(
        Guid projectId,
        string newName,
        string accentColor)
    {
        if (string.IsNullOrWhiteSpace(newName) ||
            !ProjectAccentColors.IsKnown(accentColor))
        {
            return false;
        }

        string trimmedName = newName.Trim();
        string normalizedAccentColor = ProjectAccentColors.Normalize(accentColor);

        int existingIndex = projects.FindIndex(project => project.Id == projectId);

        if (existingIndex < 0)
        {
            return false;
        }

        Project existingProject = projects[existingIndex];

        bool nameChanged = !string.Equals(
            existingProject.Name,
            trimmedName,
            StringComparison.Ordinal);

        bool colorChanged = !string.Equals(
            ProjectAccentColors.Normalize(existingProject.AccentColor),
            normalizedAccentColor,
            StringComparison.Ordinal);

        if (!nameChanged && !colorChanged)
        {
            return true;
        }

        bool wasUpdated = projectRepository.UpdateDetails(
            projectId,
            trimmedName,
            normalizedAccentColor);

        if (!wasUpdated)
        {
            return false;
        }

        projects[existingIndex] = new Project
        {
            Id = existingProject.Id,
            Name = trimmedName,
            RootPath = existingProject.RootPath,
            AccentColor = normalizedAccentColor,
            CreatedAt = existingProject.CreatedAt,
            LastScannedAt = existingProject.LastScannedAt
        };

        ProjectsChanged?.Invoke();

        return true;
    }

    public bool RemoveProject(Guid projectId)
    {
        int existingIndex = projects.FindIndex(project => project.Id == projectId);

        if (existingIndex < 0)
        {
            return false;
        }

        projectStateMemoryStore.RemoveByProjectId(projectId);
        projectRepository.Delete(projectId);
        projects.RemoveAt(existingIndex);
        ProjectsChanged?.Invoke();

        return true;
    }

    public void Clear()
    {
        projectStateMemoryStore.Clear();
        projectRepository.DeleteAll();
        projects.Clear();
        ProjectsChanged?.Invoke();
    }

    private void SortProjectsByLastScannedAt()
    {
        projects.Sort((first, second) =>
            second.LastScannedAt.CompareTo(first.LastScannedAt));
    }

    private static string NormalizeRootPath(string rootPath)
    {
        string fullPath = Path.GetFullPath(rootPath);

        string? root = Path.GetPathRoot(fullPath);

        if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        return fullPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }
}