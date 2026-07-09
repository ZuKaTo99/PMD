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
                CreatedAt = existingProject.CreatedAt,
                LastScannedAt = scannedAt
            };

            int existingIndex = projects.FindIndex(project => project.Id == existingProject.Id);

            if (existingIndex >= 0)
            {
                projects[existingIndex] = updatedProject;
            }

            projectRepository.Save(updatedProject);
            ProjectsChanged?.Invoke();

            return updatedProject;
        }

        var newProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = projectName.Trim(),
            RootPath = normalizedRootPath,
            CreatedAt = DateTime.Now,
            LastScannedAt = scannedAt
        };

        projects.Add(newProject);
        projectRepository.Save(newProject);
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

    public bool RenameProject(Guid projectId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        string trimmedName = newName.Trim();

        int existingIndex = projects.FindIndex(project => project.Id == projectId);

        if (existingIndex < 0)
        {
            return false;
        }

        Project existingProject = projects[existingIndex];

        if (string.Equals(existingProject.Name, trimmedName, StringComparison.Ordinal))
        {
            return true;
        }

        var renamedProject = new Project
        {
            Id = existingProject.Id,
            Name = trimmedName,
            RootPath = existingProject.RootPath,
            CreatedAt = existingProject.CreatedAt,
            LastScannedAt = existingProject.LastScannedAt
        };

        projects[existingIndex] = renamedProject;
        projectRepository.Rename(projectId, trimmedName);
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
        projects.Clear();
        projectRepository.DeleteAll();
        ProjectsChanged?.Invoke();
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