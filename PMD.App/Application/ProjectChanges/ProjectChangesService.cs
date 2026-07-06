using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Application.ProjectChanges;

public sealed class ProjectChangesService : IProjectChangesService
{
    public ProjectChangesResult Compare(
        ProjectState previousProjectState,
        ProjectState latestProjectState)
    {
        ArgumentNullException.ThrowIfNull(previousProjectState);
        ArgumentNullException.ThrowIfNull(latestProjectState);

        Dictionary<string, ProjectStateFile> previousFilesByPath =
            BuildFileMap(previousProjectState.Files);

        Dictionary<string, ProjectStateFile> latestFilesByPath =
            BuildFileMap(latestProjectState.Files);

        List<string> allRelativePaths = previousFilesByPath.Keys
            .Concat(latestFilesByPath.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<ProjectFileChange> changes = allRelativePaths
            .Select(relativePath => BuildFileChange(
                relativePath,
                previousFilesByPath,
                latestFilesByPath))
            .ToList();

        return new ProjectChangesResult
        {
            PreviousProjectState = previousProjectState,
            LatestProjectState = latestProjectState,
            Changes = changes
        };
    }

    private static Dictionary<string, ProjectStateFile> BuildFileMap(
        IReadOnlyList<ProjectStateFile> files)
    {
        return files
            .Where(file => !string.IsNullOrWhiteSpace(file.RelativePath))
            .GroupBy(
                file => file.RelativePath,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static ProjectFileChange BuildFileChange(
        string relativePath,
        IReadOnlyDictionary<string, ProjectStateFile> previousFilesByPath,
        IReadOnlyDictionary<string, ProjectStateFile> latestFilesByPath)
    {
        previousFilesByPath.TryGetValue(relativePath, out ProjectStateFile? previousFile);
        latestFilesByPath.TryGetValue(relativePath, out ProjectStateFile? latestFile);

        ProjectFileChangeKind changeKind = GetChangeKind(previousFile, latestFile);

        return new ProjectFileChange
        {
            RelativePath = relativePath,
            ChangeKind = changeKind,
            PreviousFile = previousFile,
            LatestFile = latestFile
        };
    }

    private static ProjectFileChangeKind GetChangeKind(
        ProjectStateFile? previousFile,
        ProjectStateFile? latestFile)
    {
        if (previousFile is null)
        {
            return ProjectFileChangeKind.Added;
        }

        if (latestFile is null)
        {
            return ProjectFileChangeKind.Removed;
        }

        if (HasFileChanged(previousFile, latestFile))
        {
            return ProjectFileChangeKind.Modified;
        }

        return ProjectFileChangeKind.Unchanged;
    }

    private static bool HasFileChanged(
        ProjectStateFile previousFile,
        ProjectStateFile latestFile)
    {
        return previousFile.SizeInBytes != latestFile.SizeInBytes ||
            previousFile.LastChangedAt != latestFile.LastChangedAt;
    }
}