namespace PMD.App.Domain.ProjectVersioning;

public sealed class ProjectVersionFileSource
{
    public string RelativePath { get; init; } = string.Empty;

    public string ExistingObjectId { get; init; } = string.Empty;

    public bool IsExecutable { get; init; }

    public bool ReusesExistingObject =>
        !string.IsNullOrWhiteSpace(ExistingObjectId);
}