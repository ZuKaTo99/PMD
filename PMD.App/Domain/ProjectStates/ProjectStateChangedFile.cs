using System;

namespace PMD.App.Domain.ProjectStates;

public sealed class ProjectStateChangedFile
{
    public string RelativePath { get; init; } = string.Empty;

    public long OldSizeInBytes {  get; init; }

    public long NewSizeInBytes { get; init; }

    public DateTime OldLastChangedAt { get; init; }

    public DateTime NewLastChangedAt { get; init; }

    public bool SizeChanged => OldSizeInBytes != NewSizeInBytes;

    public bool LastChangedAtChanged => OldLastChangedAt != NewLastChangedAt;
}