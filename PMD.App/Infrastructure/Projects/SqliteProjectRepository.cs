using PMD.App.Application.Projects;
using PMD.App.Domain.Projects;
using PMD.App.Infrastructure.Database;
using PMD.App.Infrastructure.Database.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMD.App.Infrastructure.Projects;

public sealed class SqliteProjectRepository : IProjectRepository
{
    private readonly IPmdDatabaseConnectionFactory connectionFactory;

    public SqliteProjectRepository(IPmdDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public IReadOnlyList<Project> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();

        return connection
            .Table<ProjectRecord>()
            .OrderByDescending(record => record.LastScannedAt)
            .ToList()
            .Select(MapToProject)
            .ToList();
    }

    public Project? GetById(Guid projectId)
    {
        using var connection = connectionFactory.CreateConnection();

        ProjectRecord? record = connection
            .Table<ProjectRecord>()
            .FirstOrDefault(project => project.Id == projectId.ToString());

        return record is null
            ? null
            : MapToProject(record);
    }

    public Project? GetByRootPath(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return null;
        }

        string normalizedRootPath = NormalizeRootPath(rootPath);

        using var connection = connectionFactory.CreateConnection();

        ProjectRecord? record = connection
            .Table<ProjectRecord>()
            .ToList()
            .FirstOrDefault(project =>
                string.Equals(
                    NormalizeRootPath(project.RootPath),
                    normalizedRootPath,
                    StringComparison.OrdinalIgnoreCase));

        return record is null
            ? null
            : MapToProject(record);
    }

    public void Save(Project project)
    {
        using var connection = connectionFactory.CreateConnection();

        connection.InsertOrReplace(MapToRecord(project));
    }

    public bool UpdateDetails(
        Guid projectId,
        string name,
        string accentColor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string normalizedAccentColor = ProjectAccentColors.Normalize(accentColor);

        using var connection = connectionFactory.CreateConnection();

        int affectedRows = connection.Execute(
            "UPDATE Projects SET Name = ?, AccentColor = ? WHERE Id = ?",
            name.Trim(),
            normalizedAccentColor,
            projectId.ToString());

        return affectedRows == 1;
    }

    public void Delete(Guid projectId)
    {
        using var connection = connectionFactory.CreateConnection();

        connection.Execute(
            "DELETE FROM Projects WHERE Id = ?",
            projectId.ToString());
    }

    public void DeleteAll()
    {
        using var connection = connectionFactory.CreateConnection();

        connection.DeleteAll<ProjectRecord>();
    }

    private static Project MapToProject(ProjectRecord record)
    {
        return new Project
        {
            Id = Guid.Parse(record.Id),
            Name = record.Name,
            RootPath = record.RootPath,
            AccentColor = ProjectAccentColors.Normalize(record.AccentColor),
            CreatedAt = record.CreatedAt,
            LastScannedAt = record.LastScannedAt
        };
    }

    private static ProjectRecord MapToRecord(Project project)
    {
        return new ProjectRecord
        {
            Id = project.Id.ToString(),
            Name = project.Name,
            RootPath = project.RootPath,
            AccentColor = ProjectAccentColors.Normalize(project.AccentColor),
            CreatedAt = project.CreatedAt,
            LastScannedAt = project.LastScannedAt
        };
    }

    private static string NormalizeRootPath(string rootPath)
    {
        string fullPath = Path.GetFullPath(rootPath);

        string? root = Path.GetPathRoot(fullPath);

        if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        return fullPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }
}