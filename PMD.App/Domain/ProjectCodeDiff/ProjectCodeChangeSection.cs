namespace PMD.App.Domain.ProjectCodeDiff;

public sealed class ProjectCodeChangeSection
{
    public ProjectCodeChangeKind ChangeKind { get; init; }

    public int? PreviousStartLineNumber { get; init; }

    public int? LatestStartLineNumber { get; init; }

    public IReadOnlyList<string> ContextBefore { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> PreviousLines { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> LatestLines { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> ContextAfter { get; init; } =
        Array.Empty<string>();
}