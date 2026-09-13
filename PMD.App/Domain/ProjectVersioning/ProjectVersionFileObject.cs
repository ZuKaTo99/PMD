namespace PMD.App.Domain.ProjectVersioning;

public sealed class ProjectVersionFileObject
{
    public string RelativePath { get; init; } = string.Empty;

    public string ObjectId { get; init; } = string.Empty;

    public bool IsExecutable { get; init; }
}