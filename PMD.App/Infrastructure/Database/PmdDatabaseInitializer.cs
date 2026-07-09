using PMD.App.Application.Database;
using PMD.App.Infrastructure.Database.Entities;
using SQLite;
using System.Linq;

namespace PMD.App.Infrastructure.Database;

public sealed class PmdDatabaseInitializer : IPmdDatabaseInitializer
{
    private readonly IPmdDatabaseConnectionFactory connectionFactory;

    public PmdDatabaseInitializer(IPmdDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection = connectionFactory.CreateConnection();

        connection.CreateTable<ProjectRecord>();
        connection.CreateTable<ProjectStateRecord>();
        connection.CreateTable<ProjectStateFileRecord>();

        EnsureProjectsSchema(connection);
        EnsureProjectStateFilesSchema(connection);
    }

    private static void EnsureProjectsSchema(SQLiteConnection connection)
    {
        if (!ColumnExists(connection, "Projects", "AccentColor"))
        {
            connection.Execute(
                "ALTER TABLE Projects ADD COLUMN AccentColor TEXT NOT NULL DEFAULT 'blue'");
        }
    }

    private static void EnsureProjectStateFilesSchema(SQLiteConnection connection)
    {
        if (!ColumnExists(connection, "ProjectStateFiles", "ContentHashSha256"))
        {
            connection.Execute(
                "ALTER TABLE ProjectStateFiles ADD COLUMN ContentHashSha256 TEXT NOT NULL DEFAULT ''");
        }

        if (!ColumnExists(connection, "ProjectStateFiles", "TextSnapshotContent"))
        {
            connection.Execute(
                "ALTER TABLE ProjectStateFiles ADD COLUMN TextSnapshotContent TEXT NOT NULL DEFAULT ''");
        }

        if (!ColumnExists(connection, "ProjectStateFiles", "TextSnapshotLineCount"))
        {
            connection.Execute(
                "ALTER TABLE ProjectStateFiles ADD COLUMN TextSnapshotLineCount INTEGER NOT NULL DEFAULT 0");
        }

        if (!ColumnExists(connection, "ProjectStateFiles", "TextSnapshotWasTruncated"))
        {
            connection.Execute(
                "ALTER TABLE ProjectStateFiles ADD COLUMN TextSnapshotWasTruncated INTEGER NOT NULL DEFAULT 0");
        }
    }

    private static bool ColumnExists(
        SQLiteConnection connection,
        string tableName,
        string columnName)
    {
        return connection
            .Query<DatabaseColumnInfo>($"PRAGMA table_info({tableName})")
            .Any(column => string.Equals(
                column.Name,
                columnName,
                StringComparison.OrdinalIgnoreCase));
    }

    private sealed class DatabaseColumnInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}