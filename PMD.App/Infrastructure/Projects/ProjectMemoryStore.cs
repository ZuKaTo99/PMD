using PMD.App.Application.Projects;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMD.App.Infrastructure.Projects;

public sealed class ProjectMemoryStore : IProjectMemoryStore
{
    private readonly List<Project> projects = new();

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
                Name = projectName.Trim(),
                RootPath = existingProject.RootPath,
                CreatedAt = existingProject.CreatedAt,
                LastScannedAt = scannedAt
            };

            int existingIndex = projects.FindIndex(project => project.Id == existingProject.Id);

            if (existingIndex >= 0)
            {
                projects[existingIndex] = updatedProject;
            }

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

    public void Clear()
    {
        projects.Clear();
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