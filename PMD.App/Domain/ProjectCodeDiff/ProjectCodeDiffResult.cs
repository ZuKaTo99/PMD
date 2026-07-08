using System.Linq;

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

    public int SectionCount => Sections.Count;

    public int AddedLineCount => Sections.Sum(
        section => section.LatestLines.Count);

    public int RemovedLineCount => Sections.Sum(
        section => section.PreviousLines.Count);

    public int AddedSectionCount => Sections.Count(
        section => section.ChangeKind == ProjectCodeChangeKind.Added);

    public int RemovedSectionCount => Sections.Count(
        section => section.ChangeKind == ProjectCodeChangeKind.Removed);

    public int ModifiedSectionCount => Sections.Count(
        section => section.ChangeKind == ProjectCodeChangeKind.Modified);
}