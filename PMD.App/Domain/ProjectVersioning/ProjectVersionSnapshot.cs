namespace PMD.App.Domain.ProjectVersioning;

public sealed class ProjectVersionSnapshot
{
    public Guid ProjectId { get; init; }

    public string CommitId { get; init; } = string.Empty;

    public string TreeId { get; init; } = string.Empty;

    public string ParentCommitId { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public IReadOnlyList<ProjectVersionFileObject> Files { get; init; } =
        Array.Empty<ProjectVersionFileObject>();
}