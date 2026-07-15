namespace PMD.App.Application.Analytics;

public sealed class ProjectLanguageUsage
{
    public string Name { get; init; } = string.Empty;

    public string Color { get; init; } = "#8b949e";

    public long SizeInBytes { get; init; }

    public int FileCount { get; init; }

    public double Percentage { get; init; }
}
