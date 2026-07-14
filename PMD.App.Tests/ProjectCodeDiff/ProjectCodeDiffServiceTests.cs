using PMD.App.Application.ProjectCodeDiff;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectCodeDiff;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Tests.ProjectCodeDiff;

public sealed class ProjectCodeDiffServiceTests
{
    private readonly ProjectCodeDiffService service = new();

    [Fact]
    public void BuildDiff_CreatesAddedSectionForNewFile()
    {
        ProjectFileChange fileChange = new()
        {
            RelativePath = "src/NewFile.cs",
            ChangeKind = ProjectFileChangeKind.Added,
            LatestFile = CreateFile(
                "src/NewFile.cs",
                """
                public sealed class NewFile
                {
                }
                """)
        };

        ProjectCodeDiffResult result =
            service.BuildDiff(fileChange);

        ProjectCodeChangeSection section =
            Assert.Single(result.Sections);

        Assert.Equal("src/NewFile.cs", result.RelativePath);
        Assert.Equal(ProjectCodeChangeKind.Added, section.ChangeKind);
        Assert.Equal(1, section.LatestStartLineNumber);
        Assert.Contains(
            "public sealed class NewFile",
            section.LatestLines);
        Assert.Empty(section.PreviousLines);
        Assert.Null(result.Message);
    }

    [Fact]
    public void BuildDiff_CreatesRemovedSectionForDeletedFile()
    {
        ProjectFileChange fileChange = new()
        {
            RelativePath = "src/OldFile.cs",
            ChangeKind = ProjectFileChangeKind.Removed,
            PreviousFile = CreateFile(
                "src/OldFile.cs",
                """
                public sealed class OldFile
                {
                }
                """)
        };

        ProjectCodeDiffResult result =
            service.BuildDiff(fileChange);

        ProjectCodeChangeSection section =
            Assert.Single(result.Sections);

        Assert.Equal(ProjectCodeChangeKind.Removed, section.ChangeKind);
        Assert.Equal(1, section.PreviousStartLineNumber);
        Assert.Contains(
            "public sealed class OldFile",
            section.PreviousLines);
        Assert.Empty(section.LatestLines);
        Assert.Null(result.Message);
    }

    [Fact]
    public void BuildDiff_CreatesModifiedSectionWithContext()
    {
        ProjectFileChange fileChange = new()
        {
            RelativePath = "src/Example.cs",
            ChangeKind = ProjectFileChangeKind.Modified,
            PreviousFile = CreateFile(
                "src/Example.cs",
                """
                first line
                old value
                last line
                """),
            LatestFile = CreateFile(
                "src/Example.cs",
                """
                first line
                new value
                last line
                """)
        };

        ProjectCodeDiffResult result =
            service.BuildDiff(fileChange);

        ProjectCodeChangeSection section =
            Assert.Single(result.Sections);

        Assert.Equal(ProjectCodeChangeKind.Modified, section.ChangeKind);
        Assert.Equal(2, section.PreviousStartLineNumber);
        Assert.Equal(2, section.LatestStartLineNumber);

        Assert.Equal(
            new[] { "old value" },
            section.PreviousLines);

        Assert.Equal(
            new[] { "new value" },
            section.LatestLines);

        Assert.Equal(
            new[] { "first line" },
            section.ContextBefore);

        Assert.Equal(
            new[] { "last line" },
            section.ContextAfter);

        Assert.Equal(1, result.AddedLineCount);
        Assert.Equal(1, result.RemovedLineCount);
        Assert.Equal(1, result.ModifiedSectionCount);
    }

    [Fact]
    public void BuildDiff_ReturnsMessageWhenSnapshotsAreMissing()
    {
        ProjectFileChange fileChange = new()
        {
            RelativePath = "src/Example.cs",
            ChangeKind = ProjectFileChangeKind.Modified,
            PreviousFile = CreateFile(
                "src/Example.cs",
                string.Empty),
            LatestFile = CreateFile(
                "src/Example.cs",
                string.Empty)
        };

        ProjectCodeDiffResult result =
            service.BuildDiff(fileChange);

        Assert.False(result.HasSections);
        Assert.Empty(result.Sections);
        Assert.NotNull(result.Message);

        Assert.Contains(
            "keinen gespeicherten Textauszug",
            result.Message);
    }

    [Fact]
    public void BuildDiff_RejectsSnapshotsThatAreTooLarge()
    {
        string largeSnapshot = string.Join(
            Environment.NewLine,
            Enumerable.Range(1, 601)
                .Select(index => $"Line {index}"));

        ProjectFileChange fileChange = new()
        {
            RelativePath = "src/LargeFile.cs",
            ChangeKind = ProjectFileChangeKind.Modified,
            PreviousFile = CreateFile(
                "src/LargeFile.cs",
                largeSnapshot),
            LatestFile = CreateFile(
                "src/LargeFile.cs",
                largeSnapshot + Environment.NewLine + "Changed")
        };

        ProjectCodeDiffResult result =
            service.BuildDiff(fileChange);

        Assert.False(result.HasSections);
        Assert.NotNull(result.Message);

        Assert.Contains(
            "zu groß",
            result.Message);
    }

    private static ProjectStateFile CreateFile(
        string relativePath,
        string snapshotContent)
    {
        return new ProjectStateFile
        {
            ProjectStateId = Guid.NewGuid(),
            RelativePath = relativePath,
            FileName = Path.GetFileName(relativePath),
            Extension = Path.GetExtension(relativePath),
            TextSnapshotContent = snapshotContent,
            TextSnapshotLineCount = snapshotContent.Length == 0
                ? 0
                : snapshotContent
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n')
                    .Length
        };
    }
}