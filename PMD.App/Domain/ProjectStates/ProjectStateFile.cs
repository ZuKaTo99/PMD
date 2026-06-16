namespace PMD.App.Domain.ProjectStates;

public sealed class ProjectStateFile
{
    public Guid ProjectStateId { get; init; }

    public string RelativePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string Extension { get; init; } = string.Empty;

    public long SizeInBytes { get; init; }

    public DateTime LastChangedAt { get; init; }
}