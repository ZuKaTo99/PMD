using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Projects;

public interface IProjectRepository
{
    IReadOnlyList<Project> GetAll();

    Project? GetById(Guid projectId);

    Project? GetByRootPath(string rootPath);

    void Save(Project project);

    void DeleteAll();
}