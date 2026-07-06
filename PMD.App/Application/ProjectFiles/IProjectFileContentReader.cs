using PMD.App.Domain.ProjectFiles;

namespace PMD.App.Application.ProjectFiles;

public interface IProjectFileContentReader
{
    ProjectFileContentResult ReadPreview(
        string projectRootPath,
        string relativeFilePath);
}