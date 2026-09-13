using PMD.App.Application.Database;
using PMD.App.Application.ProjectVersioning;
using PMD.App.Domain.ProjectVersioning;
using PMD.App.Infrastructure.ProjectVersioning;

namespace PMD.App.Tests.ProjectVersioning;

public sealed class LibGitProjectVersionStoreTests
    : IDisposable
{
    private readonly string testDirectoryPath;
    private readonly string projectRootPath;

    private readonly TestProjectVersionStorePathProvider
        pathProvider;

    private readonly LibGitProjectVersionStore
        versionStore;

    public LibGitProjectVersionStoreTests()
    {
        testDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "PMD.App.Tests",
            "ProjectVersioning",
            Guid.NewGuid().ToString("N"));

        projectRootPath = Path.Combine(
            testDirectoryPath,
            "project");

        string versionStoreRootPath = Path.Combine(
            testDirectoryPath,
            "app-data",
            "ProjectVersionStore");

        Directory.CreateDirectory(
            projectRootPath);

        pathProvider =
            new TestProjectVersionStorePathProvider(
                versionStoreRootPath);

        versionStore =
            new LibGitProjectVersionStore(
                pathProvider);
    }

    [Fact]
    public void PathProvider_PlacesStoreBesideDatabase()
    {
        Guid projectId = Guid.NewGuid();

        string appDataDirectoryPath = Path.Combine(
            testDirectoryPath,
            "path-provider-app-data");

        string databasePath = Path.Combine(
            appDataDirectoryPath,
            "pmd.db3");

        var databasePathProvider =
            new TestDatabasePathProvider(
                databasePath);

        var versionStorePathProvider =
            new PmdProjectVersionStorePathProvider(
                databasePathProvider);

        string versionStorePath =
            versionStorePathProvider
                .GetProjectVersionStorePath(
                    projectId);

        Assert.Equal(
            Path.Combine(
                appDataDirectoryPath,
                "ProjectVersionStore",
                $"{projectId:N}.git"),
            versionStorePath);
    }

    [Fact]
    public async Task CreateSnapshot_CreatesStoreOutsideProjectAndLeavesUserGitUntouched()
    {
        Guid projectId = Guid.NewGuid();

        string sourcePath = Path.Combine(
            projectRootPath,
            "Example.cs");

        await File.WriteAllTextAsync(
            sourcePath,
            "public sealed class Example { }");

        string userGitDirectoryPath =
            Path.Combine(
                projectRootPath,
                ".git");

        string userGitMarkerPath =
            Path.Combine(
                userGitDirectoryPath,
                "pmd-test-marker.txt");

        Directory.CreateDirectory(
            userGitDirectoryPath);

        await File.WriteAllTextAsync(
            userGitMarkerPath,
            "Darf von PMD nicht verändert werden.");

        ProjectVersionSnapshot snapshot =
            await versionStore.CreateSnapshotAsync(
                projectId,
                projectRootPath,
                [
                    CreateFileSource(
                        "Example.cs")
                ],
                new DateTimeOffset(
                    2026,
                    9,
                    13,
                    12,
                    0,
                    0,
                    TimeSpan.Zero));

        string versionStorePath =
            pathProvider
                .GetProjectVersionStorePath(
                    projectId);

        Assert.True(
            Directory.Exists(
                versionStorePath));

        Assert.True(
            File.Exists(
                Path.Combine(
                    versionStorePath,
                    "HEAD")));

        Assert.True(
            Directory.Exists(
                Path.Combine(
                    versionStorePath,
                    "objects")));

        Assert.Equal(
            "Darf von PMD nicht verändert werden.",
            await File.ReadAllTextAsync(
                userGitMarkerPath));

        Assert.Single(snapshot.Files);

        Assert.Equal(
            snapshot.CommitId,
            await versionStore
                .GetLatestCommitIdAsync(
                    projectId));
    }

    [Fact]
    public async Task CreateSnapshot_DeduplicatesIdenticalFileContent()
    {
        Guid projectId = Guid.NewGuid();

        string firstSourcePath = Path.Combine(
            projectRootPath,
            "First.cs");

        string nestedDirectoryPath = Path.Combine(
            projectRootPath,
            "src");

        string secondSourcePath = Path.Combine(
            nestedDirectoryPath,
            "Second.cs");

        Directory.CreateDirectory(
            nestedDirectoryPath);

        const string sharedContent =
            "public sealed class SharedContent { }";

        await File.WriteAllTextAsync(
            firstSourcePath,
            sharedContent);

        await File.WriteAllTextAsync(
            secondSourcePath,
            sharedContent);

        ProjectVersionSnapshot snapshot =
            await versionStore.CreateSnapshotAsync(
                projectId,
                projectRootPath,
                [
                    CreateFileSource(
                        "First.cs"),
                    CreateFileSource(
                        "src/Second.cs")
                ],
                DateTimeOffset.UtcNow);

        Assert.Equal(
            2,
            snapshot.Files.Count);

        Assert.Equal(
            snapshot.Files[0].ObjectId,
            snapshot.Files[1].ObjectId);

        Assert.True(
            await versionStore.ContainsObjectAsync(
                projectId,
                snapshot.Files[0].ObjectId));
    }

    [Fact]
    public async Task CreateSnapshot_ReusesExistingObjectWithoutReadingSourceFile()
    {
        Guid projectId = Guid.NewGuid();

        string sourcePath = Path.Combine(
            projectRootPath,
            "Reusable.cs");

        await File.WriteAllTextAsync(
            sourcePath,
            "public sealed class Reusable { }");

        ProjectVersionSnapshot firstSnapshot =
            await versionStore.CreateSnapshotAsync(
                projectId,
                projectRootPath,
                [
                    CreateFileSource(
                        "Reusable.cs")
                ],
                DateTimeOffset.UtcNow);

        ProjectVersionFileObject firstFile =
            Assert.Single(
                firstSnapshot.Files);

        File.Delete(sourcePath);

        ProjectVersionSnapshot secondSnapshot =
            await versionStore.CreateSnapshotAsync(
                projectId,
                projectRootPath,
                [
                    CreateFileSource(
                        "Reusable.cs",
                        firstFile.ObjectId)
                ],
                DateTimeOffset.UtcNow
                    .AddMinutes(1));

        ProjectVersionFileObject secondFile =
            Assert.Single(
                secondSnapshot.Files);

        Assert.Equal(
            firstFile.ObjectId,
            secondFile.ObjectId);

        Assert.Equal(
            firstSnapshot.CommitId,
            secondSnapshot.ParentCommitId);

        Assert.Equal(
            secondSnapshot.CommitId,
            await versionStore
                .GetLatestCommitIdAsync(
                    projectId));
    }

    [Fact]
    public async Task ReadObject_ReturnsOriginalBinaryContent()
    {
        Guid projectId = Guid.NewGuid();

        byte[] expectedContent =
        [
            0,
            1,
            2,
            127,
            128,
            254,
            255
        ];

        string sourcePath = Path.Combine(
            projectRootPath,
            "asset.bin");

        await File.WriteAllBytesAsync(
            sourcePath,
            expectedContent);

        ProjectVersionSnapshot snapshot =
            await versionStore.CreateSnapshotAsync(
                projectId,
                projectRootPath,
                [
                    CreateFileSource(
                        "asset.bin")
                ],
                DateTimeOffset.UtcNow);

        ProjectVersionFileObject storedFile =
            Assert.Single(
                snapshot.Files);

        byte[] loadedContent =
            await versionStore.ReadObjectAsync(
                projectId,
                storedFile.ObjectId);

        Assert.Equal(
            expectedContent,
            loadedContent);
    }

    [Fact]
    public async Task CreateSnapshot_RejectsStoreInsideUserProject()
    {
        Guid projectId = Guid.NewGuid();

        string sourcePath = Path.Combine(
            projectRootPath,
            "Example.cs");

        await File.WriteAllTextAsync(
            sourcePath,
            "public sealed class Example { }");

        string unsafeStorePath = Path.Combine(
            projectRootPath,
            ".pmd-version-store.git");

        var unsafeVersionStore =
            new LibGitProjectVersionStore(
                new FixedProjectVersionStorePathProvider(
                    unsafeStorePath));

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    unsafeVersionStore
                        .CreateSnapshotAsync(
                            projectId,
                            projectRootPath,
                            [
                                CreateFileSource(
                                    "Example.cs")
                            ],
                            DateTimeOffset.UtcNow));

        Assert.Contains(
            "darf nicht im Benutzerprojekt liegen",
            exception.Message);

        Assert.False(
            Directory.Exists(
                unsafeStorePath));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(
                    testDirectoryPath))
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

    private static ProjectVersionFileSource
        CreateFileSource(
            string relativePath,
            string existingObjectId = "")
    {
        return new ProjectVersionFileSource
        {
            RelativePath = relativePath,
            ExistingObjectId =
                existingObjectId
        };
    }

    private sealed class
        TestProjectVersionStorePathProvider
        : IProjectVersionStorePathProvider
    {
        private readonly string
            versionStoreRootPath;

        public TestProjectVersionStorePathProvider(
            string versionStoreRootPath)
        {
            this.versionStoreRootPath =
                versionStoreRootPath;
        }

        public string GetProjectVersionStorePath(
            Guid projectId)
        {
            return Path.Combine(
                versionStoreRootPath,
                $"{projectId:N}.git");
        }
    }

    private sealed class
        FixedProjectVersionStorePathProvider
        : IProjectVersionStorePathProvider
    {
        private readonly string
            versionStorePath;

        public FixedProjectVersionStorePathProvider(
            string versionStorePath)
        {
            this.versionStorePath =
                versionStorePath;
        }

        public string GetProjectVersionStorePath(
            Guid projectId)
        {
            return versionStorePath;
        }
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
}