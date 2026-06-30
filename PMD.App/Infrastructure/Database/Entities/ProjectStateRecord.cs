using SQLite;

namespace PMD.App.Infrastructure.Database.Entities;

[Table("ProjectStates")]
public sealed class ProjectStateRecord
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string ProjectId { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string RootPath { get; set; } = string.Empty;

    public DateTime ScannedAt { get; set; }

    public int FileCount { get; set; }

    public long TotalSizeInBytes { get; set; }

    public int ScanDurationInMilliseconds { get; set; }

    public int ScannedFolderCount { get; set; }

    public int SkippedFolderCount { get; set; }

    public int WarningCount { get; set; }
}