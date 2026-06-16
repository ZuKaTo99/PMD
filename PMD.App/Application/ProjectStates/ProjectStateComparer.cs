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

        var newFilePaths = newFilesByPath
            .Where(file => !oldFilesByPath.ContainsKey(file.Key))
            .Select(file => file.Value.RelativePath)
            .OrderBy(path => path)
            .ToList();

        var deletedFilePaths = oldFilesByPath
            .Where(file => !newFilesByPath.ContainsKey(file.Key))
            .Select(file => file.Value.RelativePath)
            .OrderBy(path => path)
            .ToList();

        var changedFilePaths = new List<string>();
        int unchangedFileCount = 0;

        foreach (var newFileEntry in newFilesByPath)
        {
            if (!oldFilesByPath.TryGetValue(newFileEntry.Key, out var oldFile))
            {
                continue;
            }

            if (HasFileChanged(oldFile, newFileEntry.Value))
            {
                changedFilePaths.Add(newFileEntry.Value.RelativePath);
            }
            else
            {
                unchangedFileCount++;
            }
        }

        changedFilePaths = changedFilePaths
            .OrderBy(path => path)
            .ToList();

        return new ProjectStateComparisonResult
        {
            OldProjectStateId = oldState.Id,
            NewProjectStateId = newState.Id,
            NewFileCount = newFilePaths.Count,
            ChangedFileCount = changedFilePaths.Count,
            DeletedFileCount = deletedFilePaths.Count,
            UnchangedFileCount = unchangedFileCount,
            NewFilePaths = newFilePaths,
            ChangedFilePaths = changedFilePaths,
            DeletedFilePaths = deletedFilePaths
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