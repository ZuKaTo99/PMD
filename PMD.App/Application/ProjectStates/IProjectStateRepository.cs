using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateRepository
{
    IReadOnlyList<ProjectState> GetLatest(int maxCount);

    IReadOnlyList<ProjectState> GetByProjectId(Guid projectId, int maxCount);

    void Save(ProjectState projectState);

    void DeleteAll();
}