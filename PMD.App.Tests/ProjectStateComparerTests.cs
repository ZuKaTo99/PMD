using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Tests.ProjectStates;

public sealed class ProjectStateComparerTests
{
    private static readonly DateTime ScanTime =
        new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Compare_RecognizesNewFile()
    {
        ProjectState oldState = CreateState();
        ProjectState newState = CreateState(
            CreateFile("src/NewFile.cs", 120));

        ProjectStateComparisonResult result =
            ProjectStateComparer.Compare(oldState, newState);

        Assert.Equal(1, result.NewFileCount);
        Assert.Equal(0, result.ChangedFileCount);
        Assert.Equal(0, result.DeletedFileCount);
        Assert.Equal(0, result.UnchangedFileCount);
        Assert.Equal("src/NewFile.cs", Assert.Single(result.NewFilePaths));
    }

    [Fact]
    public void Compare_RecognizesChangedFile()
    {
        ProjectState oldState = CreateState(
            CreateFile("src/ChangedFile.cs", 120));

        ProjectState newState = CreateState(
            CreateFile("src/ChangedFile.cs", 240));

        ProjectStateComparisonResult result =
            ProjectStateComparer.Compare(oldState, newState);

        Assert.Equal(0, result.NewFileCount);
        Assert.Equal(1, result.ChangedFileCount);
        Assert.Equal(0, result.DeletedFileCount);
        Assert.Equal(0, result.UnchangedFileCount);

        ProjectStateChangedFile changedFile =
            Assert.Single(result.ChangedFiles);

        Assert.Equal("src/ChangedFile.cs", changedFile.RelativePath);
        Assert.Equal(120L, changedFile.OldSizeInBytes);
        Assert.Equal(240L, changedFile.NewSizeInBytes);
        Assert.True(changedFile.SizeChanged);
    }

    [Fact]
    public void Compare_RecognizesDeletedFile()
    {
        ProjectState oldState = CreateState(
            CreateFile("src/DeletedFile.cs", 120));

        ProjectState newState = CreateState();

        ProjectStateComparisonResult result =
            ProjectStateComparer.Compare(oldState, newState);

        Assert.Equal(0, result.NewFileCount);
        Assert.Equal(0, result.ChangedFileCount);
        Assert.Equal(1, result.DeletedFileCount);
        Assert.Equal(0, result.UnchangedFileCount);
        Assert.Equal(
            "src/DeletedFile.cs",
            Assert.Single(result.DeletedFilePaths));
    }

    [Fact]
    public void Compare_RecognizesUnchangedFile()
    {
        ProjectState oldState = CreateState(
            CreateFile("src/UnchangedFile.cs", 120));

        ProjectState newState = CreateState(
            CreateFile("src/UnchangedFile.cs", 120));

        ProjectStateComparisonResult result =
            ProjectStateComparer.Compare(oldState, newState);

        Assert.Equal(0, result.TotalChangeCount);
        Assert.Equal(1, result.UnchangedFileCount);
        Assert.Empty(result.NewFilePaths);
        Assert.Empty(result.ChangedFilePaths);
        Assert.Empty(result.DeletedFilePaths);
    }

    private static ProjectState CreateState(
        params ProjectStateFile[] files)
    {
        return new ProjectState
        {
            ProjectId = Guid.NewGuid(),
            ProjectName = "Test project",
            RootPath = @"C:\Projects\Test",
            ScannedAt = ScanTime,
            FileCount = files.Length,
            Files = files.ToList()
        };
    }

    private static ProjectStateFile CreateFile(
        string relativePath,
        long sizeInBytes)
    {
        return new ProjectStateFile
        {
            RelativePath = relativePath,
            FileName = Path.GetFileName(relativePath),
            Extension = Path.GetExtension(relativePath),
            SizeInBytes = sizeInBytes,
            LastChangedAt = ScanTime
        };
    }
}
