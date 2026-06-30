using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;
using PMD.App.Infrastructure.Database;
using PMD.App.Infrastructure.Database.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMD.App.Infrastructure.ProjectStates;

public sealed class SqliteProjectStateRepository : IProjectStateRepository
{
    private readonly IPmdDatabaseConnectionFactory connectionFactory;

    public SqliteProjectStateRepository(IPmdDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public IReadOnlyList<ProjectState> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();

        List<ProjectStateRecord> projectStateRecords = connection
            .Table<ProjectStateRecord>()
            .OrderByDescending(record => record.ScannedAt)
            .ToList();

        List<ProjectStateFileRecord> fileRecords = connection
            .Table<ProjectStateFileRecord>()
            .ToList();

        Dictionary<string, List<ProjectStateFileRecord>> filesByProjectStateId = fileRecords
            .GroupBy(file => file.ProjectStateId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.OrdinalIgnoreCase);

        return projectStateRecords
            .Select(record => MapToProjectState(record, filesByProjectStateId))
            .ToList();
    }

    public void Save(ProjectState projectState)
    {
        ArgumentNullException.ThrowIfNull(projectState);

        using var connection = connectionFactory.CreateConnection();

        connection.InsertOrReplace(MapToProjectStateRecord(projectState));

        connection.Execute(
            "DELETE FROM ProjectStateFiles WHERE ProjectStateId = ?",
            projectState.Id.ToString());

        List<ProjectStateFileRecord> fileRecords = projectState.Files
            .Select(file => MapToFileRecord(projectState, file))
            .ToList();

        if (fileRecords.Count > 0)
        {
            connection.InsertAll(fileRecords);
        }
    }

    public void DeleteAll()
    {
        using var connection = connectionFactory.CreateConnection();

        connection.DeleteAll<ProjectStateFileRecord>();
        connection.DeleteAll<ProjectStateRecord>();
    }

    private static ProjectState MapToProjectState(
        ProjectStateRecord record,
        IReadOnlyDictionary<string, List<ProjectStateFileRecord>> filesByProjectStateId)
    {
        filesByProjectStateId.TryGetValue(record.Id, out List<ProjectStateFileRecord>? fileRecords);

        Guid projectStateId = Guid.Parse(record.Id);

        return new ProjectState
        {
            Id = projectStateId,
            ProjectId = Guid.Parse(record.ProjectId),
            ProjectName = record.ProjectName,
            RootPath = record.RootPath,
            CreatedAt = record.CreatedAt,
            ScannedAt = record.ScannedAt,
            ScanDuration = TimeSpan.FromMilliseconds(record.ScanDurationInMilliseconds),
            FileCount = record.FileCount,
            ScannedFolderCount = record.ScannedFolderCount,
            IgnoredFolderCount = record.SkippedFolderCount,
            WarningCount = record.WarningCount,
            TotalSizeInBytes = record.TotalSizeInBytes,
            Files = (fileRecords ?? new List<ProjectStateFileRecord>())
                .Select(file => MapToProjectStateFile(projectStateId, file))
                .ToList()
        };
    }

    private static ProjectStateFile MapToProjectStateFile(
        Guid projectStateId,
        ProjectStateFileRecord record)
    {
        return new ProjectStateFile
        {
            ProjectStateId = projectStateId,
            RelativePath = record.RelativePath,
            FileName = record.FileName,
            Extension = record.Extension,
            SizeInBytes = record.SizeInBytes,
            LastChangedAt = record.LastChangedAt
        };
    }

    private static ProjectStateRecord MapToProjectStateRecord(ProjectState projectState)
    {
        return new ProjectStateRecord
        {
            Id = projectState.Id.ToString(),
            ProjectId = projectState.ProjectId.ToString(),
            ProjectName = projectState.ProjectName,
            RootPath = projectState.RootPath,
            CreatedAt = projectState.CreatedAt,
            ScannedAt = projectState.ScannedAt,
            FileCount = projectState.FileCount,
            TotalSizeInBytes = projectState.TotalSizeInBytes,
            ScanDurationInMilliseconds = (int)projectState.ScanDuration.TotalMilliseconds,
            ScannedFolderCount = projectState.ScannedFolderCount,
            SkippedFolderCount = projectState.IgnoredFolderCount,
            WarningCount = projectState.WarningCount
        };
    }

    private static ProjectStateFileRecord MapToFileRecord(
        ProjectState projectState,
        ProjectStateFile file)
    {
        return new ProjectStateFileRecord
        {
            Id = Guid.NewGuid().ToString(),
            ProjectStateId = projectState.Id.ToString(),
            RelativePath = file.RelativePath,
            FullPath = Path.Combine(projectState.RootPath, file.RelativePath),
            FileName = file.FileName,
            Extension = file.Extension,
            SizeInBytes = file.SizeInBytes,
            LastChangedAt = file.LastChangedAt
        };
    }
}