using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Projects;

public interface IProjectMemoryStore
{
    IReadOnlyList<Project> Projects { get; }

    Project RememberScannedProject(
        string projectName,
        string rootPath,
        DateTime scannedAt);

    Project? GetProjectById(Guid projectId);

    Project? GetProjectByRootPath(string rootPath);

    void Clear();
}