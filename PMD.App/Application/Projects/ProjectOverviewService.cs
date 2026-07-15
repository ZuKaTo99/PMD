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

        List<ProjectState> projectStates = GetProjectStates(project)
            .ToList();

        LoadRequiredProjectStateFiles(projectStates);

        ProjectState? latestProjectState =
            projectStates.FirstOrDefault();

        ProjectState? previousProjectState =
            projectStates.Skip(1).FirstOrDefault();

        ProjectStateComparisonResult? changesSinceLastCheck = null;

        if (latestProjectState is not null &&
            previousProjectState is not null)
        {
            changesSinceLastCheck = ProjectStateComparer.Compare(
                previousProjectState,
                latestProjectState);
        }

        return new ProjectOverview
        {
            Project = project,
            ProjectStates = projectStates,
            ContentSummary = CreateContentSummary(
                latestProjectState),
            ChangesSinceLastCheck = changesSinceLastCheck
        };
    }

    private void LoadRequiredProjectStateFiles(
    List<ProjectState> projectStates)
    {
        int projectStatesToLoad = Math.Min(
            2,
            projectStates.Count);

        for (int index = 0;
             index < projectStatesToLoad;
             index++)
        {
            projectStates[index] =
                projectStateStore.LoadFiles(
                    projectStates[index]);
        }
    }

    private IReadOnlyList<ProjectState> GetProjectStates(Project project)
    {
        return projectStateStore
            .GetByProjectId(project.Id, int.MaxValue)
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

        var allFileTypes = latestProjectState.Files
            .GroupBy(file => NormalizeExtension(file.Extension))
            .Select(group => new ProjectFileTypeSummary(
                group.Key,
                group.Count(),
                group.Sum(file => file.SizeInBytes)))
            .OrderByDescending(fileType => fileType.FileCount)
            .ThenBy(fileType => fileType.Extension)
            .ToList();

        var frequentFileTypes = allFileTypes
            .Take(8)
            .ToList();

        int differentFileTypeCount = allFileTypes.Count;

        var largerFiles = latestProjectState.Files
            .OrderByDescending(file => file.SizeInBytes)
            .ThenBy(file => file.RelativePath)
            .Take(5)
            .Select(CreateFileHighlight)
            .ToList();

        int codeFileCount = CountFilesByCategory(latestProjectState, CodeExtensions);
        int configFileCount = CountFilesByCategory(latestProjectState, ConfigExtensions);
        int textFileCount = CountFilesByCategory(latestProjectState, TextExtensions);
        int knownFileCount = codeFileCount + configFileCount + textFileCount;
        int otherFileCount = Math.Max(0, latestProjectState.FileCount - knownFileCount);

        return new ProjectContentSummary
        {
            ProfileLabel = BuildProfileLabel(latestProjectState, codeFileCount),
            ShortDescription = BuildShortDescription(latestProjectState, allFileTypes, codeFileCount),
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
            AllFileTypes = allFileTypes,
            FileGroups = CreateFileGroups(latestProjectState),
            LargerFiles = largerFiles
        };
    }

    private static IReadOnlyList<ProjectFileGroupSummary> CreateFileGroups(ProjectState projectState)
    {
        var codeFiles = GetFilesByCategory(projectState, FileCategory.Code);
        var configFiles = GetFilesByCategory(projectState, FileCategory.Config);
        var textFiles = GetFilesByCategory(projectState, FileCategory.Text);
        var otherFiles = GetFilesByCategory(projectState, FileCategory.Other);

        return new[]
        {
            CreateFileGroup(
                "Code und Projekt",
                "Quellcode, UI-Dateien und Projektdateien.",
                codeFiles),
            CreateFileGroup(
                "Konfiguration",
                "Einstellungen, Skripte und technische Begleitdateien.",
                configFiles),
            CreateFileGroup(
                "Text und Doku",
                "Dokumentation, Notizen und Textdateien.",
                textFiles),
            CreateFileGroup(
                "Weitere Dateien",
                "Dateien, die PMD aktuell keiner Hauptgruppe zuordnet.",
                otherFiles)
        };
    }

    private static ProjectFileGroupSummary CreateFileGroup(
        string title,
        string description,
        IReadOnlyList<ProjectStateFile> files)
    {
        return new ProjectFileGroupSummary(
            title,
            description,
            files.Count,
            files
                .Take(8)
                .Select(CreateFileHighlight)
                .ToList());
    }

    private static IReadOnlyList<ProjectStateFile> GetFilesByCategory(
        ProjectState projectState,
        FileCategory category)
    {
        return projectState.Files
            .Where(file => GetFileCategory(file) == category)
            .OrderBy(file => file.RelativePath)
            .ToList();
    }

    private static FileCategory GetFileCategory(ProjectStateFile file)
    {
        string extension = NormalizeExtension(file.Extension);

        if (CodeExtensions.Contains(extension))
        {
            return FileCategory.Code;
        }

        if (ConfigExtensions.Contains(extension))
        {
            return FileCategory.Config;
        }

        if (TextExtensions.Contains(extension))
        {
            return FileCategory.Text;
        }

        return FileCategory.Other;
    }

    private static ProjectFileHighlight CreateFileHighlight(ProjectStateFile file)
    {
        return new ProjectFileHighlight(
            file.FileName,
            file.RelativePath,
            NormalizeExtension(file.Extension),
            file.SizeInBytes,
            file.LastChangedAt);
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
        IReadOnlyList<ProjectFileTypeSummary> allFileTypes,
        int codeFileCount)
    {
        string mainFileType = allFileTypes.FirstOrDefault()?.Extension ?? "unbekannte Dateien";

        if (codeFileCount > 0)
        {
            return $"PMD erkennt {projectState.FileCount} Dateien in {allFileTypes.Count} Dateitypen. Davon sind {codeFileCount} Code- und Projektdateien. Häufigster Typ ist {mainFileType}.";
        }

        return $"PMD erkennt {projectState.FileCount} Dateien in {allFileTypes.Count} Dateitypen. Häufigster Typ ist {mainFileType}.";
    }

    private static string NormalizeExtension(string? extension)
    {
        return string.IsNullOrWhiteSpace(extension)
            ? "ohne Endung"
            : extension;
    }

    private enum FileCategory
    {
        Code,
        Config,
        Text,
        Other
    }
}
