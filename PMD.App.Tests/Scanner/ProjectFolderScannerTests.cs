using PMD.App.Domain.Scanner;
using PMD.App.Infrastructure.Scanner;
using System.Security.Cryptography;
using System.Text;

namespace PMD.App.Tests.Scanner;

public sealed class ProjectFolderScannerTests : IDisposable
{
    private readonly string testRootPath;

    public ProjectFolderScannerTests()
    {
        testRootPath = Path.Combine(
            Path.GetTempPath(),
            "PMD.App.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(testRootPath);
    }

    [Fact]
    public async Task ScanFolderAsync_CreatesFileMetadataHashAndTextSnapshot()
    {
        // Arrange
        const string fileContent =
            """
            public sealed class Example
            {
            }
            """;

        string filePath = Path.Combine(
            testRootPath,
            "Example.cs");

        await File.WriteAllTextAsync(
            filePath,
            fileContent,
            Encoding.UTF8);

        var scanner = new ProjectFolderScanner();

        // Act
        ProjectFolderScanResult result =
            await scanner.ScanFolderAsync(testRootPath);

        // Assert
        ProjectFileEntry scannedFile = Assert.Single(result.Files);

        Assert.Equal("Example.cs", scannedFile.FileName);
        Assert.Equal("Example.cs", scannedFile.RelativePath);
        Assert.Equal(".cs", scannedFile.Extension);
        Assert.False(string.IsNullOrWhiteSpace(
            scannedFile.ContentHashSha256));

        string expectedHash = Convert
            .ToHexString(
                SHA256.HashData(
                    await File.ReadAllBytesAsync(filePath)))
            .ToLowerInvariant();

        Assert.Equal(
            expectedHash,
            scannedFile.ContentHashSha256);

        Assert.Contains(
            "public sealed class Example",
            scannedFile.TextSnapshotContent);

        Assert.True(scannedFile.TextSnapshotLineCount > 0);
        Assert.False(scannedFile.TextSnapshotWasTruncated);
    }

    [Fact]
    public async Task ScanFolderAsync_IgnoresBuildFolders()
    {
        // Arrange
        string sourceFolder = Path.Combine(
            testRootPath,
            "Source");

        string binFolder = Path.Combine(
            testRootPath,
            "bin");

        string objFolder = Path.Combine(
            testRootPath,
            "obj");

        Directory.CreateDirectory(sourceFolder);
        Directory.CreateDirectory(binFolder);
        Directory.CreateDirectory(objFolder);

        await File.WriteAllTextAsync(
            Path.Combine(sourceFolder, "Included.cs"),
            "public sealed class Included { }");

        await File.WriteAllTextAsync(
            Path.Combine(binFolder, "Ignored.cs"),
            "public sealed class IgnoredBin { }");

        await File.WriteAllTextAsync(
            Path.Combine(objFolder, "Ignored.cs"),
            "public sealed class IgnoredObj { }");

        var scanner = new ProjectFolderScanner();

        // Act
        ProjectFolderScanResult result =
            await scanner.ScanFolderAsync(testRootPath);

        // Assert
        ProjectFileEntry scannedFile = Assert.Single(result.Files);

        Assert.Equal(
            Path.Combine("Source", "Included.cs"),
            scannedFile.RelativePath);

        Assert.Contains(
            result.IgnoredFolders,
            folder => string.Equals(
                folder,
                "bin",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            result.IgnoredFolders,
            folder => string.Equals(
                folder,
                "obj",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanFolderAsync_ReportsProgress()
    {
        // Arrange
        for (int index = 1; index <= 3; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(
                    testRootPath,
                    $"File{index}.txt"),
                $"Testinhalt {index}");
        }

        var scanner = new ProjectFolderScanner();
        var progressReports =
            new List<ProjectFolderScanProgress>();

        var progress =
            new InlineProgress<ProjectFolderScanProgress>(
                progressReports.Add);

        // Act
        ProjectFolderScanResult result =
            await scanner.ScanFolderAsync(
                testRootPath,
                progress);

        // Assert
        Assert.Equal(3, result.Files.Count);
        Assert.NotEmpty(progressReports);

        ProjectFolderScanProgress finalProgress =
            progressReports[^1];

        Assert.Equal(3, finalProgress.FoundFileCount);
        Assert.Equal(3, finalProgress.ProcessedFileCount);
        Assert.True(finalProgress.ScannedFolderCount >= 1);
    }

    [Fact]
    public async Task ScanFolderAsync_ThrowsWhenCancellationWasRequested()
    {
        // Arrange
        var scanner = new ProjectFolderScanner();
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act and assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scanner.ScanFolderAsync(
                testRootPath,
                cancellationToken:
                    cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        if (!Directory.Exists(testRootPath))
        {
            return;
        }

        Directory.Delete(
            testRootPath,
            recursive: true);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> reportAction;

        public InlineProgress(Action<T> reportAction)
        {
            this.reportAction = reportAction;
        }

        public void Report(T value)
        {
            reportAction(value);
        }
    }
}