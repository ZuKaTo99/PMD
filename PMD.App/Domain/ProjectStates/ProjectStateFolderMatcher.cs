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

        string firstPath = NormalizeFolderPath(firstState.RootPath);
        string secondPath = NormalizeFolderPath(secondState.RootPath);

        return string.Equals(
            firstPath,
            secondPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        return folderPath
            .Trim()
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}