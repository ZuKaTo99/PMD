using System;

namespace PMD.App.Domain.Projects;

public sealed class Project
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;

    public string RootPath { get; init; } = string.Empty;

    public string AccentColor { get; init; } = ProjectAccentColors.Default;

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public DateTime LastScannedAt { get; init; }
}