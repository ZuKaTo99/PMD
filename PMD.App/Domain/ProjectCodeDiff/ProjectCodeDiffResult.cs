namespace PMD.App.Domain.ProjectCodeDiff;

public sealed class ProjectCodeDiffResult
{
    public string RelativePath { get; init; } = string.Empty;

    public string? Message { get; init; }

    public bool PreviousSnapshotWasTruncated { get; init; }

    public bool LatestSnapshotWasTruncated { get; init; }

    public IReadOnlyList<ProjectCodeChangeSection> Sections { get; init; } =
        Array.Empty<ProjectCodeChangeSection>();

    public bool HasSections => Sections.Count > 0;
}