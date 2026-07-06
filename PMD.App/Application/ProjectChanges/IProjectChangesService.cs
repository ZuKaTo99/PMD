using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Application.ProjectChanges;

public interface IProjectChangesService
{
    ProjectChangesResult Compare(
        ProjectState previousProjectState,
        ProjectState latestProjectState);
}