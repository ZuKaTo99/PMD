using PMD.App.Domain.ProjectStates;
using System.Collections.Generic;

namespace PMD.App.Application.ProjectStates;

public interface IProjectStateRepository
{
    IReadOnlyList<ProjectState> GetAll();

    void Save(ProjectState projectState);

    void DeleteAll();
}