using PMD.App.Application.Database;
using PMD.App.Infrastructure.Database.Entities;
using SQLite;
using System;
using System.Linq;

namespace PMD.App.Infrastructure.Database;

public sealed class PmdDatabaseInitializer : IPmdDatabaseInitializer
{
    private const int CurrentSchemaVersion = 3;

    private readonly IPmdDatabaseConnectionFactory connectionFactory;

    public PmdDatabaseInitializer(
        IPmdDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection =
            connectionFactory.CreateConnection();

        int schemaVersion =
            GetSchemaVersion(connection);

        if (schemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Die PMD-Datenbank verwendet Schema-Version " +
                $"{schemaVersion}. Diese Anwendung unterstützt höchstens " +
                $"Version {CurrentSchemaVersion}.");
        }

        while (schemaVersion < CurrentSchemaVersion)
        {
            int targetVersion = schemaVersion + 1;

            connection.RunInTransaction(() =>
            {
                ApplyMigration(
                    connection,
                    targetVersion);

                SetSchemaVersion(
                    connection,
                    targetVersion);
            });

            schemaVersion = targetVersion;
        }
    }

    private static void ApplyMigration(
        SQLiteConnection connection,
        int targetVersion)
    {
        switch (targetVersion)
        {
            case 1:
                ApplyVersion1Migration(connection);
                break;

            case 2:
                ApplyVersion2Migration(connection);
                break;

            case 3:
                ApplyVersion3Migration(connection);
                break;

            default:
                throw new InvalidOperationException(
                    $"Für Schema-Version {targetVersion} " +
                    "ist keine Datenbankmigration vorhanden.");
        }
    }

    private static void ApplyVersion1Migration(
        SQLiteConnection connection)
    {
        connection.CreateTable<ProjectRecord>();
        connection.CreateTable<ProjectStateRecord>();
        connection.CreateTable<ProjectStateFileRecord>();

        EnsureProjectsSchema(connection);
        EnsureProjectStateFilesSchema(connection);
    }

    private static void ApplyVersion2Migration(
        SQLiteConnection connection)
    {
        connection.Execute(
            """
            CREATE INDEX IF NOT EXISTS IX_Projects_LastScannedAt
            ON Projects (LastScannedAt DESC)
            """);

        connection.Execute(
            """
            CREATE INDEX IF NOT EXISTS IX_ProjectStates_ScannedAt
            ON ProjectStates (ScannedAt DESC)
            """);

        connection.Execute(
            """
            CREATE INDEX IF NOT EXISTS IX_ProjectStates_ProjectId_ScannedAt
            ON ProjectStates (ProjectId, ScannedAt DESC)
            """);

        connection.Execute(
            """
            CREATE INDEX IF NOT EXISTS IX_ProjectStateFiles_ProjectStateId_RelativePath
            ON ProjectStateFiles (ProjectStateId, RelativePath)
            """);
    }

    private static void ApplyVersion3Migration(
        SQLiteConnection connection)
    {
        connection.CreateTable<KanbanTaskRecord>();

        connection.Execute(
            """
            CREATE INDEX IF NOT EXISTS IX_KanbanTasks_Status_SortOrder
            ON KanbanTasks (Status, SortOrder)
            """);

        connection.Execute(
            """
            CREATE INDEX IF NOT EXISTS IX_KanbanTasks_ProjectId
            ON KanbanTasks (ProjectId)
            """);
    }

    private static void EnsureProjectsSchema(
        SQLiteConnection connection)
    {
        if (!ColumnExists(
                connection,
                "Projects",
                "AccentColor"))
        {
            connection.Execute(
                """
                ALTER TABLE Projects
                ADD COLUMN AccentColor TEXT NOT NULL DEFAULT 'blue'
                """);
        }
    }

    private static void EnsureProjectStateFilesSchema(
        SQLiteConnection connection)
    {
        if (!ColumnExists(
                connection,
                "ProjectStateFiles",
                "ContentHashSha256"))
        {
            connection.Execute(
                """
                ALTER TABLE ProjectStateFiles
                ADD COLUMN ContentHashSha256 TEXT NOT NULL DEFAULT ''
                """);
        }

        if (!ColumnExists(
                connection,
                "ProjectStateFiles",
                "TextSnapshotContent"))
        {
            connection.Execute(
                """
                ALTER TABLE ProjectStateFiles
                ADD COLUMN TextSnapshotContent TEXT NOT NULL DEFAULT ''
                """);
        }

        if (!ColumnExists(
                connection,
                "ProjectStateFiles",
                "TextSnapshotLineCount"))
        {
            connection.Execute(
                """
                ALTER TABLE ProjectStateFiles
                ADD COLUMN TextSnapshotLineCount INTEGER NOT NULL DEFAULT 0
                """);
        }

        if (!ColumnExists(
                connection,
                "ProjectStateFiles",
                "TextSnapshotWasTruncated"))
        {
            connection.Execute(
                """
                ALTER TABLE ProjectStateFiles
                ADD COLUMN TextSnapshotWasTruncated INTEGER NOT NULL DEFAULT 0
                """);
        }
    }

    private static int GetSchemaVersion(
        SQLiteConnection connection)
    {
        return connection.ExecuteScalar<int>(
            "PRAGMA user_version");
    }

    private static void SetSchemaVersion(
        SQLiteConnection connection,
        int schemaVersion)
    {
        connection.Execute(
            $"PRAGMA user_version = {schemaVersion}");
    }

    private static bool ColumnExists(
        SQLiteConnection connection,
        string tableName,
        string columnName)
    {
        return connection
            .Query<DatabaseColumnInfo>(
                $"PRAGMA table_info({tableName})")
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