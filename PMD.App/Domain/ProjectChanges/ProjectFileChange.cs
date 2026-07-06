using PMD.App.Domain.ProjectStates;

namespace PMD.App.Domain.ProjectChanges;

public sealed class ProjectFileChange
{
    public string RelativePath { get; init; } = string.Empty;

    public ProjectFileChangeKind ChangeKind { get; init; }

    public ProjectStateFile? PreviousFile { get; init; }

    public ProjectStateFile? LatestFile { get; init; }

    public string FileName =>
        LatestFile?.FileName ??
        PreviousFile?.FileName ??
        string.Empty;

    public string Extension =>
        LatestFile?.Extension ??
        PreviousFile?.Extension ??
        string.Empty;

    public long? PreviousSizeInBytes => PreviousFile?.SizeInBytes;

    public long? LatestSizeInBytes => LatestFile?.SizeInBytes;
}