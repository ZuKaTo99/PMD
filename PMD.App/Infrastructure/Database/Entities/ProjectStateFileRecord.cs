using SQLite;

namespace PMD.App.Infrastructure.Database.Entities;

[Table("ProjectStateFiles")]
public sealed class ProjectStateFileRecord
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string ProjectStateId { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DateTime LastChangedAt { get; set; }

    public string ContentHashSha256 { get; set; } = string.Empty;

    public string TextSnapshotContent { get; set; } = string.Empty;

    public int TextSnapshotLineCount { get; set; }

    public bool TextSnapshotWasTruncated { get; set; }
}