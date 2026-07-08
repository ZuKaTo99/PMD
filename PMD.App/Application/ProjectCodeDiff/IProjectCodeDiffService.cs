using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectCodeDiff;

namespace PMD.App.Application.ProjectCodeDiff;

public interface IProjectCodeDiffService
{
    ProjectCodeDiffResult BuildDiff(ProjectFileChange fileChange);
}