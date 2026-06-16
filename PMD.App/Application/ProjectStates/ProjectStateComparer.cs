using System;
using System.Linq;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Application.ProjectStates;

public static class ProjectStateComparer
{
    public static ProjectStateComparisonResult Compare(
        ProjectState oldState,
        ProjectState newState)
    {
        ArgumentNullException.ThrowIfNull(oldState);
        ArgumentNullException.ThrowIfNull(newState);

        var oldFilesByPath = oldState.Files
            .ToDictionary(
                file => NormalizePath(file.RelativePath),
                StringComparer.OrdinalIgnoreCase);

        var newFilesByPath = newState.Files
            .ToDictionary(
                file => NormalizePath(file.RelativePath),
                StringComparer.OrdinalIgnoreCase);

        int newFileCount = newFilesByPath.Keys
            .Count(path => !oldFilesByPath.ContainsKey(path));

        int deletedFileCount = oldFilesByPath.Keys
            .Count(path => !newFilesByPath.ContainsKey(path));

        int changedFileCount = 0;
        int unchangedFileCount = 0;

        foreach (var newFileEntry in newFilesByPath)
        {
            if (!oldFilesByPath.TryGetValue(newFileEntry.Key, out var oldFile))
            {
                continue;
            }

            if (HasFileChanged(oldFile, newFileEntry.Value))
            {
                changedFileCount++;
            }
            else
            {
                unchangedFileCount++;
            }
        }

        return new ProjectStateComparisonResult
        {
            OldProjectStateId = oldState.Id,
            NewProjectStateId = newState.Id,
            NewFileCount = newFileCount,
            ChangedFileCount = changedFileCount,
            DeletedFileCount = deletedFileCount,
            UnchangedFileCount = unchangedFileCount
        };
    }

    private static bool HasFileChanged(
        ProjectStateFile oldFile,
        ProjectStateFile newFile)
    {
        return oldFile.SizeInBytes != newFile.SizeInBytes
            || oldFile.LastChangedAt != newFile.LastChangedAt;
    }

    private static string NormalizePath(string relativePath)
    {
        return relativePath
            .Replace('\\', '/')
            .Trim();
    }
}