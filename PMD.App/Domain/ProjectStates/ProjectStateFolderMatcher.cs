using System;
using System.IO;

namespace PMD.App.Domain.ProjectStates;

public static class ProjectStateFolderMatcher
{
    public static bool IsSameProjectFolder(
        ProjectState firstState,
        ProjectState secondState)
    {
        ArgumentNullException.ThrowIfNull(firstState);
        ArgumentNullException.ThrowIfNull(secondState);

        return IsSameProjectFolder(firstState, secondState.RootPath);
    }

    public static bool IsSameProjectFolder(
        ProjectState projectState,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(projectState);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        string projectStateRootPath = NormalizeFolderPath(projectState.RootPath);
        string comparedRootPath = NormalizeFolderPath(rootPath);

        return string.Equals(
            projectStateRootPath,
            comparedRootPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        string fullPath = Path.GetFullPath(folderPath.Trim());
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