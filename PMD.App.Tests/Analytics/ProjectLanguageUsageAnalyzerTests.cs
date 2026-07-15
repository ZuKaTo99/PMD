using PMD.App.Application.Analytics;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Tests.Analytics;

public sealed class ProjectLanguageUsageAnalyzerTests
{
    [Fact]
    public void Analyze_UsesFileSizeForLanguagePercentages()
    {
        ProjectStateFile[] files =
        [
            CreateFile("src/App.cs", 700),
            CreateFile("src/Home.razor", 200),
            CreateFile("wwwroot/app.css", 100)
        ];

        IReadOnlyList<ProjectLanguageUsage> result =
            ProjectLanguageUsageAnalyzer.Analyze(files);

        Assert.Equal("C#", result[0].Name);
        Assert.Equal(70d, result[0].Percentage, 3);
        Assert.Equal("HTML", result[1].Name);
        Assert.Equal(20d, result[1].Percentage, 3);
        Assert.Equal("CSS", result[2].Name);
        Assert.Equal(10d, result[2].Percentage, 3);
    }

    [Fact]
    public void Analyze_IgnoresVendoredAndGeneratedFiles()
    {
        ProjectStateFile[] files =
        [
            CreateFile("src/App.cs", 100),
            CreateFile("wwwroot/lib/bootstrap/bootstrap.min.css", 10_000),
            CreateFile("Generated/Bindings.g.cs", 5_000)
        ];

        ProjectLanguageUsage language = Assert.Single(
            ProjectLanguageUsageAnalyzer.Analyze(files));

        Assert.Equal("C#", language.Name);
        Assert.Equal(100d, language.Percentage, 3);
        Assert.Equal(1, language.FileCount);
    }

    [Fact]
    public void Combine_RecalculatesPercentagesAcrossProjects()
    {
        IReadOnlyList<ProjectLanguageUsage> firstProject =
            ProjectLanguageUsageAnalyzer.Analyze(
            [
                CreateFile("src/App.cs", 300),
                CreateFile("web/app.js", 100)
            ]);

        IReadOnlyList<ProjectLanguageUsage> secondProject =
            ProjectLanguageUsageAnalyzer.Analyze(
            [
                CreateFile("src/Worker.cs", 100),
                CreateFile("web/site.css", 500)
            ]);

        IReadOnlyList<ProjectLanguageUsage> result =
            ProjectLanguageUsageAnalyzer.Combine(
            [
                firstProject,
                secondProject
            ]);

        Assert.Equal("CSS", result[0].Name);
        Assert.Equal(50d, result[0].Percentage, 3);
        Assert.Equal("C#", result[1].Name);
        Assert.Equal(40d, result[1].Percentage, 3);
        Assert.Equal("JavaScript", result[2].Name);
        Assert.Equal(10d, result[2].Percentage, 3);
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
            SizeInBytes = sizeInBytes
        };
    }
}
