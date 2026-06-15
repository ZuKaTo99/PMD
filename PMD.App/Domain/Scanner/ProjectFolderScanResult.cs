namespace PMD.App.Domain.Scanner;

public sealed class ProjectFolderScanResult
{

    public string ProjectName { get; init; } = string.Empty;
    public string RootPath { get; init; } = string.Empty;

    public DateTime ScannedAt { get; init; } = DateTime.Now;

    public TimeSpan ScanDuration { get; init; }

    public int ScannedFolderCount { get; init; }

    public List<ProjectFileEntry> Files { get; init; } = new();

    public List<string> IgnoredFolders { get; init; } = new();

    public List<string> Warnings { get; init; } = new();

    public int FileCount => Files.Count;

    public int IgnoredFolderCount => IgnoredFolders.Count;

    public int WarningCount => Warnings.Count;

    public long TotalSizeInBytes => Files.Sum(file => file.SizeInBytes);
}