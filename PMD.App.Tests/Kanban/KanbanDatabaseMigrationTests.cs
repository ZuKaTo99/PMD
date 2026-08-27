using PMD.App.Application.Database;
using PMD.App.Domain.Kanban;
using PMD.App.Infrastructure.Database;
using PMD.App.Infrastructure.Kanban;
using SQLite;

namespace PMD.App.Tests.Kanban;

public sealed class KanbanDatabaseMigrationTests : IDisposable
{
    private readonly string testDirectoryPath;
    private readonly string databasePath;

    public KanbanDatabaseMigrationTests()
    {
        testDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "PMD.App.Tests",
            "KanbanMigration",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(testDirectoryPath);

        databasePath = Path.Combine(
            testDirectoryPath,
            "pmd-kanban-migration-tests.db");
    }

    [Fact]
    public void Initialize_MigratesVersion3AndPreservesExistingTasks()
    {
        Guid taskId = Guid.NewGuid();

        IPmdDatabasePathProvider pathProvider =
            new TestDatabasePathProvider(databasePath);

        var connectionFactory =
            new PmdDatabaseConnectionFactory(pathProvider);

        using (SQLiteConnection connection =
               connectionFactory.CreateConnection())
        {
            connection.Execute(
                """
                CREATE TABLE KanbanTasks
                (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    ProjectId TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    Priority INTEGER NOT NULL,
                    SortOrder INTEGER NOT NULL,
                    CreatedAt DATETIME NOT NULL,
                    UpdatedAt DATETIME NOT NULL
                )
                """);

            connection.Execute(
                """
                INSERT INTO KanbanTasks
                (
                    Id,
                    Title,
                    Description,
                    ProjectId,
                    Status,
                    Priority,
                    SortOrder,
                    CreatedAt,
                    UpdatedAt
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                taskId.ToString(),
                "Bestehende Aufgabe",
                "Muss die Migration überstehen.",
                string.Empty,
                (int)KanbanTaskStatus.Open,
                (int)KanbanTaskPriority.Normal,
                0,
                new DateTime(2026, 7, 15, 12, 0, 0),
                new DateTime(2026, 7, 15, 12, 0, 0));

            connection.Execute(
                "PRAGMA user_version = 3");
        }

        new PmdDatabaseInitializer(
            connectionFactory).Initialize();

        using (SQLiteConnection connection =
               connectionFactory.CreateConnection())
        {
            Assert.Equal(
                5,
                connection.ExecuteScalar<int>(
                    "PRAGMA user_version"));

            List<DatabaseColumnName> columns = connection
                .Query<DatabaseColumnName>(
                    "PRAGMA table_info(KanbanTasks)");

            Assert.Contains(
                columns,
                column =>
                    column.Name == "DueDate");

            Assert.Contains(
                columns,
                column =>
                    column.Name ==
                    "LinkedFileRelativePath");
        }

        var repository =
            new SqliteKanbanTaskRepository(
                connectionFactory);

        KanbanTask? migratedTask =
            repository.GetById(taskId);

        Assert.NotNull(migratedTask);

        Assert.Equal(
            "Bestehende Aufgabe",
            migratedTask!.Title);

        Assert.Null(migratedTask.DueDate);

        Assert.Empty(
            migratedTask.LinkedFileRelativePath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(testDirectoryPath))
            {
                Directory.Delete(
                    testDirectoryPath,
                    recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class TestDatabasePathProvider
        : IPmdDatabasePathProvider
    {
        private readonly string path;

        public TestDatabasePathProvider(string path)
        {
            this.path = path;
        }

        public string GetDatabasePath()
        {
            return path;
        }
    }

    private sealed class DatabaseColumnName
    {
        [Column("name")]
        public string Name { get; set; } =
            string.Empty;
    }
}