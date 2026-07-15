using SQLite;
using System;

namespace PMD.App.Infrastructure.Database.Entities;

[Table("KanbanTasks")]
public sealed class KanbanTaskRecord
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ProjectId { get; set; } = string.Empty;

    public int Status { get; set; }

    public int Priority { get; set; }

    public int SortOrder { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
