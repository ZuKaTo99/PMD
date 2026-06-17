using PMD.App.Domain.ProjectStates;

namespace PMD.App.Features.Scanner.Pages;

public partial class ProjectScannerPage
{
    private static bool IsSameProjectFolder(
        ProjectState oldState,
        ProjectState newState)
    {
        string oldPath = NormalizeFolderPath(oldState.RootPath);
        string newPath = NormalizeFolderPath(newState.RootPath);

        return string.Equals(
            oldPath,
            newPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        return folderPath
            .Trim()
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}