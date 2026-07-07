namespace PMD.App.Domain.ProjectStates;

public sealed class ProjectStateFile
{
    public Guid ProjectStateId { get; init; }

    public string RelativePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string Extension { get; init; } = string.Empty;

    public long SizeInBytes { get; init; }

    public DateTime LastChangedAt { get; init; }

    public string ContentHashSha256 { get; init; } = string.Empty;

    public string TextSnapshotContent { get; init; } = string.Empty;

    public int TextSnapshotLineCount { get; init; }

    public bool TextSnapshotWasTruncated { get; init; }

    public bool HasTextSnapshot =>
        !string.IsNullOrEmpty(TextSnapshotContent);
}