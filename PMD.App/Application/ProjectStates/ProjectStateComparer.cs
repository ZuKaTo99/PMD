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

        var changedFiles = oldFilesByPath
            .Where(oldFilePair =>
                newFilesByPath.TryGetValue(oldFilePair.Key, out var newFile)
                && HasFileChanged(oldFilePair.Value, newFile))
            .Select(oldFilePair =>
            {
                var oldFile = oldFilePair.Value;
                var newFile = newFilesByPath[oldFilePair.Key];

                return new ProjectStateChangedFile
                {
                    RelativePath = newFile.RelativePath,
                    OldSizeInBytes = oldFile.SizeInBytes,
                    NewSizeInBytes = newFile.SizeInBytes,
                    OldLastChangedAt = oldFile.LastChangedAt,
                    NewLastChangedAt = newFile.LastChangedAt
                };
            })
            .OrderBy(file => file.RelativePath)
            .ToList();

        var changedFilePaths = changedFiles
            .Select(file => file.RelativePath)
            .ToList();

        var unchangedFileCount = oldFilesByPath
            .Count(oldFilePair =>
                newFilesByPath.TryGetValue(oldFilePair.Key, out var newFile)
                && !HasFileChanged(oldFilePair.Value, newFile));

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
            DeletedFilePaths = deletedFilePaths,
            ChangedFiles = changedFiles
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