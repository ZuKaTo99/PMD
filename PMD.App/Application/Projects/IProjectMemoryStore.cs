using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Projects;

public interface IProjectMemoryStore
{
    event Action? ProjectsChanged;

    IReadOnlyList<Project> Projects { get; }

    Project RememberScannedProject(
        string projectName,
        string rootPath,
        DateTime scannedAt);

    Project? GetProjectById(Guid projectId);

    Project? GetProjectByRootPath(string rootPath);

    bool RenameProject(Guid projectId, string newName);

    bool ChangeProjectAccentColor(Guid projectId, string accentColor);

    bool RemoveProject(Guid projectId);

    void Clear();
}