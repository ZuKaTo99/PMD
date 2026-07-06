using PMD.App.Domain.ProjectStates;

namespace PMD.App.Domain.ProjectChanges;

public sealed class ProjectChangesResult
{
    public ProjectState PreviousProjectState { get; init; } = default!;

    public ProjectState LatestProjectState { get; init; } = default!;

    public IReadOnlyList<ProjectFileChange> Changes { get; init; } =
        Array.Empty<ProjectFileChange>();

    public IReadOnlyList<ProjectFileChange> AddedFiles =>
        GetChanges(ProjectFileChangeKind.Added);

    public IReadOnlyList<ProjectFileChange> ModifiedFiles =>
        GetChanges(ProjectFileChangeKind.Modified);

    public IReadOnlyList<ProjectFileChange> RemovedFiles =>
        GetChanges(ProjectFileChangeKind.Removed);

    public IReadOnlyList<ProjectFileChange> UnchangedFiles =>
        GetChanges(ProjectFileChangeKind.Unchanged);

    public int AddedFileCount => AddedFiles.Count;

    public int ModifiedFileCount => ModifiedFiles.Count;

    public int RemovedFileCount => RemovedFiles.Count;

    public int UnchangedFileCount => UnchangedFiles.Count;

    public int TotalFileCount => Changes.Count;

    private IReadOnlyList<ProjectFileChange> GetChanges(ProjectFileChangeKind changeKind)
    {
        return Changes
            .Where(change => change.ChangeKind == changeKind)
            .OrderBy(change => change.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}