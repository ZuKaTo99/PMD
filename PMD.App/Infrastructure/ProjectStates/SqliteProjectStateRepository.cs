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

    public IReadOnlyList<ProjectState> GetLatest(int maxCount)
    {
        using var connection = connectionFactory.CreateConnection();

        List<ProjectStateRecord> records = connection
            .Table<ProjectStateRecord>()
            .OrderByDescending(record => record.ScannedAt)
            .Take(maxCount)
            .ToList();

        return LoadProjectStatesWithFiles(records);
    }

    public ProjectState? GetLatestByProjectId(Guid projectId)
    {
        using var connection = connectionFactory.CreateConnection();

        string projectIdText = projectId.ToString();

        ProjectStateRecord? record = connection
            .Table<ProjectStateRecord>()
            .Where(record => record.ProjectId == projectIdText)
            .OrderByDescending(record => record.ScannedAt)
            .FirstOrDefault();

        if (record is null)
        {
            return null;
        }

        return LoadProjectStatesWithFiles(new[] { record })
            .FirstOrDefault();
    }

    public IReadOnlyList<ProjectState> GetByProjectId(Guid projectId, int maxCount)
    {
        using var connection = connectionFactory.CreateConnection();

        string projectIdText = projectId.ToString();

        List<ProjectStateRecord> records = connection
            .Table<ProjectStateRecord>()
            .Where(record => record.ProjectId == projectIdText)
            .OrderByDescending(record => record.ScannedAt)
            .Take(maxCount)
            .ToList();

        return LoadProjectStatesWithFiles(records);
    }

    public IReadOnlyList<ProjectStateFile> GetFilesByProjectStateId(Guid projectStateId)
    {
        using var connection = connectionFactory.CreateConnection();

        string projectStateIdText = projectStateId.ToString();

        return connection
            .Table<ProjectStateFileRecord>()
            .Where(record => record.ProjectStateId == projectStateIdText)
            .OrderBy(record => record.RelativePath)
            .ToList()
            .Select(record => MapToProjectStateFile(projectStateId, record))
            .ToList();
    }

    public void Save(ProjectState projectState)
    {
        ArgumentNullException.ThrowIfNull(projectState);

        ProjectStateRecord projectStateRecord =
            MapToProjectStateRecord(projectState);

        List<ProjectStateFileRecord> fileRecords = projectState.Files
            .Select(file => MapToFileRecord(projectState, file))
            .ToList();

        string projectStateId = projectState.Id.ToString();

        using var connection = connectionFactory.CreateConnection();

        connection.RunInTransaction(() =>
        {
            connection.InsertOrReplace(projectStateRecord);

            connection.Execute(
                "DELETE FROM ProjectStateFiles WHERE ProjectStateId = ?",
                projectStateId);

            if (fileRecords.Count > 0)
            {
                connection.InsertAll(
                    fileRecords,
                    false);
            }
        });
    }

    public void DeleteByProjectId(Guid projectId)
    {
        using var connection = connectionFactory.CreateConnection();

        string projectIdText = projectId.ToString();

        List<string> projectStateIds = connection
            .Table<ProjectStateRecord>()
            .Where(record => record.ProjectId == projectIdText)
            .Select(record => record.Id)
            .ToList();

        if (projectStateIds.Count == 0)
        {
            return;
        }

        string placeholders = string.Join(", ", projectStateIds.Select(_ => "?"));

        connection.Execute(
            $"DELETE FROM ProjectStateFiles WHERE ProjectStateId IN ({placeholders})",
            projectStateIds.Cast<object>().ToArray());

        connection.Execute(
            "DELETE FROM ProjectStates WHERE ProjectId = ?",
            projectIdText);
    }

    public void DeleteAll()
    {
        using var connection = connectionFactory.CreateConnection();

        connection.DeleteAll<ProjectStateFileRecord>();
        connection.DeleteAll<ProjectStateRecord>();
    }

    private IReadOnlyList<ProjectState> LoadProjectStatesWithFiles(
        IReadOnlyList<ProjectStateRecord> records)
    {
        if (records.Count == 0)
        {
            return Array.Empty<ProjectState>();
        }

        using var connection = connectionFactory.CreateConnection();

        string[] projectStateIds = records
            .Select(record => record.Id)
            .ToArray();

        string placeholders = string.Join(", ", projectStateIds.Select(_ => "?"));

        List<ProjectStateFileRecord> fileRecords = connection.Query<ProjectStateFileRecord>(
            $"SELECT * FROM ProjectStateFiles WHERE ProjectStateId IN ({placeholders})",
            projectStateIds.Cast<object>().ToArray());

        Dictionary<string, List<ProjectStateFileRecord>> filesByProjectStateId = fileRecords
            .GroupBy(file => file.ProjectStateId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.OrdinalIgnoreCase);

        return records
            .Select(record => MapToProjectState(record, filesByProjectStateId))
            .ToList();
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
            LastChangedAt = record.LastChangedAt,
            ContentHashSha256 = record.ContentHashSha256 ?? string.Empty,
            TextSnapshotContent = record.TextSnapshotContent ?? string.Empty,
            TextSnapshotLineCount = record.TextSnapshotLineCount,
            TextSnapshotWasTruncated = record.TextSnapshotWasTruncated
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
            LastChangedAt = file.LastChangedAt,
            ContentHashSha256 = file.ContentHashSha256,
            TextSnapshotContent = file.TextSnapshotContent,
            TextSnapshotLineCount = file.TextSnapshotLineCount,
            TextSnapshotWasTruncated = file.TextSnapshotWasTruncated
        };
    }
}