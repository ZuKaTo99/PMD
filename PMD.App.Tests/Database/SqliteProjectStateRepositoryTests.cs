using PMD.App.Application.Database;
using PMD.App.Domain.ProjectStates;
using PMD.App.Infrastructure.Database;
using PMD.App.Infrastructure.ProjectStates;
using SQLite;

namespace PMD.App.Tests.Database;

public sealed class SqliteProjectStateRepositoryTests : IDisposable
{
    private readonly string testDirectoryPath;
    private readonly IPmdDatabaseConnectionFactory connectionFactory;
    private readonly SqliteProjectStateRepository repository;

    public SqliteProjectStateRepositoryTests()
    {
        testDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "PMD.App.Tests",
            "Database",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(testDirectoryPath);

        string databasePath = Path.Combine(
            testDirectoryPath,
            "pmd-tests.db");

        var pathProvider =
            new TestDatabasePathProvider(databasePath);

        connectionFactory =
            new PmdDatabaseConnectionFactory(pathProvider);

        var databaseInitializer =
            new PmdDatabaseInitializer(connectionFactory);

        databaseInitializer.Initialize();

        repository =
            new SqliteProjectStateRepository(connectionFactory);
    }

    [Fact]
    public void Initialize_CreatesCurrentSchemaAndIndexes()
    {
        using SQLiteConnection connection =
            connectionFactory.CreateConnection();

        int schemaVersion = connection.ExecuteScalar<int>(
            "PRAGMA user_version");

        List<DatabaseObjectName> indexes = connection
            .Query<DatabaseObjectName>(
                """
                SELECT name
                FROM sqlite_master
                WHERE type = 'index'
                """);

        Assert.Equal(2, schemaVersion);

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_Projects_LastScannedAt");

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_ProjectStates_ScannedAt");

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_ProjectStates_ProjectId_ScannedAt");

        Assert.Contains(
            indexes,
            index =>
                index.Name ==
                "IX_ProjectStateFiles_ProjectStateId_RelativePath");
    }

    [Fact]
    public void Save_PersistsProjectStateMetadataAndFiles()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();

        ProjectState projectState =
            CreateProjectState(
                projectId,
                "Example.cs",
                "hash-example");

        // Act
        repository.Save(projectState);

        ProjectState? loadedProjectState =
            repository.GetLatestByProjectId(projectId);

        IReadOnlyList<ProjectStateFile> loadedFiles =
            repository.GetFilesByProjectStateId(
                projectState.Id);

        // Assert
        Assert.NotNull(loadedProjectState);

        Assert.Equal(
            projectState.Id,
            loadedProjectState!.Id);

        Assert.Equal(
            projectState.ProjectId,
            loadedProjectState.ProjectId);

        Assert.Equal(
            projectState.FileCount,
            loadedProjectState.FileCount);

        Assert.Empty(loadedProjectState.Files);

        ProjectStateFile loadedFile =
            Assert.Single(loadedFiles);

        Assert.Equal(
            "Example.cs",
            loadedFile.FileName);

        Assert.Equal(
            "hash-example",
            loadedFile.ContentHashSha256);

        Assert.Contains(
            "public sealed class Example",
            loadedFile.TextSnapshotContent);
    }

    [Fact]
    public void Save_RollsBackWhenFileInsertFails()
    {
        // Arrange
        Guid projectId = Guid.NewGuid();

        ProjectState projectState =
            CreateProjectState(
                projectId,
                "Failure.cs",
                "hash-failure");

        using (SQLiteConnection connection =
               connectionFactory.CreateConnection())
        {
            connection.Execute(
                """
                CREATE TRIGGER ForceProjectStateFileInsertFailure
                BEFORE INSERT ON ProjectStateFiles
                BEGIN
                    SELECT RAISE(
                        ABORT,
                        'forced file insert failure');
                END
                """);
        }

        // Act
        Assert.ThrowsAny<SQLiteException>(
            () => repository.Save(projectState));

        // Assert
        using SQLiteConnection verificationConnection =
            connectionFactory.CreateConnection();

        int storedProjectStateCount =
            verificationConnection.ExecuteScalar<int>(
                """
                SELECT COUNT(*)
                FROM ProjectStates
                WHERE Id = ?
                """,
                projectState.Id.ToString());

        int storedFileCount =
            verificationConnection.ExecuteScalar<int>(
                """
                SELECT COUNT(*)
                FROM ProjectStateFiles
                WHERE ProjectStateId = ?
                """,
                projectState.Id.ToString());

        Assert.Equal(0, storedProjectStateCount);
        Assert.Equal(0, storedFileCount);
    }

    [Fact]
    public void DeleteByProjectId_RemovesOnlyMatchingProjectData()
    {
        // Arrange
        Guid firstProjectId = Guid.NewGuid();
        Guid secondProjectId = Guid.NewGuid();

        ProjectState firstProjectState =
            CreateProjectState(
                firstProjectId,
                "First.cs",
                "hash-first");

        ProjectState secondProjectState =
            CreateProjectState(
                secondProjectId,
                "Second.cs",
                "hash-second");

        repository.Save(firstProjectState);
        repository.Save(secondProjectState);

        // Act
        repository.DeleteByProjectId(firstProjectId);

        // Assert
        Assert.Empty(
            repository.GetByProjectId(
                firstProjectId,
                10));

        Assert.Empty(
            repository.GetFilesByProjectStateId(
                firstProjectState.Id));

        ProjectState? remainingProjectState =
            repository.GetLatestByProjectId(
                secondProjectId);

        Assert.NotNull(remainingProjectState);

        ProjectStateFile remainingFile =
            Assert.Single(
                repository.GetFilesByProjectStateId(
                    secondProjectState.Id));

        Assert.Equal(
            "Second.cs",
            remainingFile.FileName);
    }

    public void Dispose()
    {
        if (!Directory.Exists(testDirectoryPath))
        {
            return;
        }

        Directory.Delete(
            testDirectoryPath,
            recursive: true);
    }

    private static ProjectState CreateProjectState(
        Guid projectId,
        string fileName,
        string contentHash)
    {
        Guid projectStateId = Guid.NewGuid();

        string relativePath = Path.Combine(
            "src",
            fileName);

        string className =
            Path.GetFileNameWithoutExtension(fileName);

        string snapshotContent =
            $$"""
    public sealed class {{className}}
    {
    }
    """;

        var file = new ProjectStateFile
        {
            ProjectStateId = projectStateId,
            RelativePath = relativePath,
            FileName = fileName,
            Extension = Path.GetExtension(fileName),
            SizeInBytes = snapshotContent.Length,
            LastChangedAt = DateTime.UtcNow,
            ContentHashSha256 = contentHash,
            TextSnapshotContent = snapshotContent,
            TextSnapshotLineCount = 3,
            TextSnapshotWasTruncated = false
        };

        return new ProjectState
        {
            Id = projectStateId,
            ProjectId = projectId,
            ProjectName = "Testprojekt",
            RootPath = Path.GetTempPath(),
            CreatedAt = DateTime.UtcNow,
            ScannedAt = DateTime.UtcNow,
            ScanDuration = TimeSpan.FromMilliseconds(125),
            FileCount = 1,
            ScannedFolderCount = 2,
            IgnoredFolderCount = 1,
            WarningCount = 0,
            TotalSizeInBytes = file.SizeInBytes,
            Files = new List<ProjectStateFile>
            {
                file
            }
        };
    }

    private sealed class TestDatabasePathProvider
        : IPmdDatabasePathProvider
    {
        private readonly string databasePath;

        public TestDatabasePathProvider(
            string databasePath)
        {
            this.databasePath = databasePath;
        }

        public string GetDatabasePath()
        {
            return databasePath;
        }
    }

    private sealed class DatabaseObjectName
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}