using SQLite;

namespace PMD.App.Infrastructure.Database.Entities;

[Table("Projects")]
public sealed class ProjectRecord
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string RootPath { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime LastScannedAt { get; set; }
}