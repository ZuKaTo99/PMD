using System.Data;

namespace PMD.App.Domain.ProjectStates;

public sealed class ProjectStateFile
{
    public Guid ProjectStateId { get; set; }

    public string RelativPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DataSetDateTime LastChangedAt { get; set; }
}