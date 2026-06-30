using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Application.Projects;

public sealed class ProjectOverviewService : IProjectOverviewService
{
    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".razor", ".xaml", ".css", ".html", ".js", ".ts", ".json", ".xml",
        ".csproj", ".sln", ".slnx"
    };

    private static readonly HashSet<string> ConfigExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".config", ".props", ".targets", ".editorconfig", ".user", ".cmd", ".ps1", ".yml", ".yaml"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".txt", ".log", ".csv"
    };

    private readonly IProjectMemoryStore projectStore;
    private readonly IProjectStateMemoryStore projectStateStore;

    public ProjectOverviewService(
        IProjectMemoryStore projectStore,
        IProjectStateMemoryStore projectStateStore)
    {
        this.projectStore = projectStore;
        this.projectStateStore = projectStateStore;
    }

    public ProjectOverview? GetProjectOverview(Guid projectId)
    {
        Project? project = projectStore.GetProjectById(projectId);

        if (project is null)
        {
            return null;
        }

        IReadOnlyList<ProjectState> projectStates = GetProjectStates(project);
        ProjectState? latestProjectState = projectStates.FirstOrDefault();
        ProjectState? previousProjectState = projectStates.Skip(1).FirstOrDefault();

        ProjectStateComparisonResult? changesSinceLastCheck = null;

        if (latestProjectState is not null && previousProjectState is not null)
        {
            changesSinceLastCheck = ProjectStateComparer.Compare(
                previousProjectState,
                latestProjectState);
        }

        return new ProjectOverview
        {
            Project = project,
            ProjectStates = projectStates,
            ContentSummary = CreateContentSummary(latestProjectState),
            ChangesSinceLastCheck = changesSinceLastCheck
        };
    }

    private IReadOnlyList<ProjectState> GetProjectStates(Project project)
    {
        return projectStateStore.ProjectStates
            .Where(projectState => BelongsToProject(projectState, project))
            .GroupBy(projectState => projectState.Id)
            .Select(group => group.First())
            .OrderByDescending(projectState => projectState.ScannedAt)
            .ThenByDescending(projectState => projectState.CreatedAt)
            .ToList();
    }

    private static ProjectContentSummary CreateContentSummary(ProjectState? latestProjectState)
    {
        if (latestProjectState is null)
        {
            return ProjectContentSummary.Empty;
        }

        var frequentFileTypes = latestProjectState.Files
            .GroupBy(file => NormalizeExtension(file.Extension))
            .Select(group => new ProjectFileTypeSummary(
                group.Key,
                group.Count(),
                group.Sum(file => file.SizeInBytes)))
            .OrderByDescending(fileType => fileType.FileCount)
            .ThenBy(fileType => fileType.Extension)
            .Take(8)
            .ToList();

        int differentFileTypeCount = latestProjectState.Files
            .Select(file => NormalizeExtension(file.Extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var largerFiles = latestProjectState.Files
            .OrderByDescending(file => file.SizeInBytes)
            .ThenBy(file => file.RelativePath)
            .Take(5)
            .Select(file => new ProjectFileHighlight(
                file.FileName,
                file.RelativePath,
                NormalizeExtension(file.Extension),
                file.SizeInBytes,
                file.LastChangedAt))
            .ToList();

        int codeFileCount = CountFilesByCategory(latestProjectState, CodeExtensions);
        int configFileCount = CountFilesByCategory(latestProjectState, ConfigExtensions);
        int textFileCount = CountFilesByCategory(latestProjectState, TextExtensions);
        int knownFileCount = codeFileCount + configFileCount + textFileCount;
        int otherFileCount = Math.Max(0, latestProjectState.FileCount - knownFileCount);

        return new ProjectContentSummary
        {
            ProfileLabel = BuildProfileLabel(latestProjectState, codeFileCount),
            ShortDescription = BuildShortDescription(latestProjectState, frequentFileTypes, codeFileCount),
            FileCount = latestProjectState.FileCount,
            TotalSizeInBytes = latestProjectState.TotalSizeInBytes,
            ScannedFolderCount = latestProjectState.ScannedFolderCount,
            IgnoredFolderCount = latestProjectState.IgnoredFolderCount,
            WarningCount = latestProjectState.WarningCount,
            ScanDuration = latestProjectState.ScanDuration,
            DifferentFileTypeCount = differentFileTypeCount,
            CodeFileCount = codeFileCount,
            ConfigFileCount = configFileCount,
            TextFileCount = textFileCount,
            OtherFileCount = otherFileCount,
            FrequentFileTypes = frequentFileTypes,
            LargerFiles = largerFiles
        };
    }

    private static int CountFilesByCategory(
        ProjectState projectState,
        HashSet<string> extensions)
    {
        return projectState.Files.Count(file => extensions.Contains(NormalizeExtension(file.Extension)));
    }

    private static string BuildProfileLabel(
        ProjectState projectState,
        int codeFileCount)
    {
        bool hasCSharpFiles = projectState.Files.Any(file =>
            string.Equals(NormalizeExtension(file.Extension), ".cs", StringComparison.OrdinalIgnoreCase));

        bool hasRazorFiles = projectState.Files.Any(file =>
            string.Equals(NormalizeExtension(file.Extension), ".razor", StringComparison.OrdinalIgnoreCase));

        bool hasProjectFile = projectState.Files.Any(file =>
            string.Equals(NormalizeExtension(file.Extension), ".csproj", StringComparison.OrdinalIgnoreCase));

        if (hasCSharpFiles && hasProjectFile)
        {
            return hasRazorFiles
                ? ".NET / Blazor Projekt"
                : ".NET / C# Projekt";
        }

        if (codeFileCount > 0)
        {
            return "Code-Projekt";
        }

        return "Projektordner";
    }

    private static string BuildShortDescription(
        ProjectState projectState,
        IReadOnlyList<ProjectFileTypeSummary> frequentFileTypes,
        int codeFileCount)
    {
        string mainFileType = frequentFileTypes.FirstOrDefault()?.Extension ?? "unbekannte Dateien";

        if (codeFileCount > 0)
        {
            return $"PMD erkennt {projectState.FileCount} Dateien, davon {codeFileCount} Code- und Projektdateien. Häufigster Typ ist {mainFileType}.";
        }

        return $"PMD erkennt {projectState.FileCount} Dateien. Häufigster Typ ist {mainFileType}.";
    }

    private static bool BelongsToProject(
        ProjectState projectState,
        Project project)
    {
        if (projectState.ProjectId == project.Id)
        {
            return true;
        }

        return ProjectStateFolderMatcher.IsSameProjectFolder(
            projectState,
            project.RootPath);
    }

    private static string NormalizeExtension(string? extension)
    {
        return string.IsNullOrWhiteSpace(extension)
            ? "ohne Endung"
            : extension;
    }
}
