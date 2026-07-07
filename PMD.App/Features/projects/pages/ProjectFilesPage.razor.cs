using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectFiles;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectFiles;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectFilesPage
{
    protected const string SortByPath = "path";
    protected const string SortByExtension = "extension";
    protected const string SortBySizeDescending = "size-desc";
    protected const string SortByLastChangedDescending = "last-changed-desc";

    private const int MaxVisibleFileCount = 100;

    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectFileContentReader ProjectFileContentReader { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    [SupplyParameterFromQuery(Name = "datei")]
    public string? RequestedFilePath { get; set; }

    protected Project? CurrentProject { get; private set; }

    protected ProjectState? LatestProjectState { get; private set; }

    protected IReadOnlyList<ProjectStateFile> Files { get; private set; } =
        Array.Empty<ProjectStateFile>();

    protected ProjectStateFile? SelectedFile { get; private set; }

    protected ProjectFileContentResult? SelectedFileContentResult { get; private set; }

    protected string SearchText { get; private set; } = string.Empty;

    protected string SelectedExtension { get; private set; } = string.Empty;

    protected string SelectedSortMode { get; private set; } = SortByPath;

    protected bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        !string.IsNullOrWhiteSpace(SelectedExtension);

    protected string SelectedSortLabel => SelectedSortMode switch
    {
        SortByExtension => "Dateityp",
        SortBySizeDescending => "Größe",
        SortByLastChangedDescending => "Änderungsdatum",
        _ => "Pfad"
    };

    protected IReadOnlyList<string> AvailableExtensions => Files
        .Select(file => file.Extension)
        .Where(extension => !string.IsNullOrWhiteSpace(extension))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
        .ToList();

    protected IReadOnlyList<ProjectStateFile> FilteredFiles => Files
        .Where(MatchesSelectedExtension)
        .Where(MatchesSearchText)
        .ToList();

    protected IReadOnlyList<ProjectStateFile> VisibleFiles => SortFiles(FilteredFiles)
        .Take(MaxVisibleFileCount)
        .ToList();

    protected override void OnParametersSet()
    {
        LoadProjectFiles();
    }

    protected void OnSearchTextChanged(ChangeEventArgs eventArgs)
    {
        SearchText = eventArgs.Value?.ToString() ?? string.Empty;
        ClearSelectedFileIfFilteredOut();
    }

    protected void OnSelectedExtensionChanged(ChangeEventArgs eventArgs)
    {
        SelectedExtension = eventArgs.Value?.ToString() ?? string.Empty;
        ClearSelectedFileIfFilteredOut();
    }

    protected void OnSelectedSortModeChanged(ChangeEventArgs eventArgs)
    {
        SelectedSortMode = eventArgs.Value?.ToString() ?? SortByPath;
    }

    protected void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedExtension = string.Empty;
    }

    protected void SelectFile(ProjectStateFile file)
    {
        if (!IsSameFile(file, SelectedFile))
        {
            SelectedFileContentResult = null;
        }

        SelectedFile = file;
    }

    protected void LoadSelectedFilePreview()
    {
        if (SelectedFile is null || LatestProjectState is null)
        {
            SelectedFileContentResult = ProjectFileContentResult.Blocked(
                string.Empty,
                "Es wurde keine Datei ausgewählt.");

            return;
        }

        SelectedFileContentResult = ProjectFileContentReader.ReadPreview(
            LatestProjectState.RootPath,
            SelectedFile.RelativePath);
    }

    private void LoadProjectFiles()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
        LatestProjectState = null;
        Files = Array.Empty<ProjectStateFile>();
        SelectedFile = null;
        SelectedFileContentResult = null;
        SearchText = string.Empty;
        SelectedExtension = string.Empty;
        SelectedSortMode = SortByPath;

        if (CurrentProject is null)
        {
            return;
        }

        LatestProjectState = ProjectStateMemoryStore.GetLatestByProjectId(ProjectId);

        if (LatestProjectState is null)
        {
            return;
        }

        Files = ProjectStateMemoryStore.GetFilesByProjectStateId(LatestProjectState.Id);

        TrySelectRequestedFile();

    }

    private IEnumerable<ProjectStateFile> SortFiles(IReadOnlyList<ProjectStateFile> files)
    {
        return SelectedSortMode switch
        {
            SortByExtension => files
                .OrderBy(file => file.Extension, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase),

            SortBySizeDescending => files
                .OrderByDescending(file => file.SizeInBytes)
                .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase),

            SortByLastChangedDescending => files
                .OrderByDescending(file => file.LastChangedAt)
                .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase),

            _ => files
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
        };
    }

    private void ClearSelectedFileIfFilteredOut()
    {
        if (SelectedFile is null)
        {
            return;
        }

        bool selectedFileStillVisible = FilteredFiles
            .Any(file => IsSameFile(file, SelectedFile));

        if (!selectedFileStillVisible)
        {
            SelectedFile = null;
            SelectedFileContentResult = null;
        }
    }

    private bool MatchesSelectedExtension(ProjectStateFile file)
    {
        if (string.IsNullOrWhiteSpace(SelectedExtension))
        {
            return true;
        }

        return string.Equals(
            file.Extension,
            SelectedExtension,
            StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSearchText(ProjectStateFile file)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return file.RelativePath.Contains(
                SearchText,
                StringComparison.OrdinalIgnoreCase) ||
            file.FileName.Contains(
                SearchText,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameFile(
        ProjectStateFile firstFile,
        ProjectStateFile? secondFile)
    {
        if (secondFile is null)
        {
            return false;
        }

        return string.Equals(
                firstFile.RelativePath,
                secondFile.RelativePath,
                StringComparison.OrdinalIgnoreCase) &&
            firstFile.LastChangedAt == secondFile.LastChangedAt &&
            firstFile.SizeInBytes == secondFile.SizeInBytes;
    }

    private void TrySelectRequestedFile()
    {
        if (string.IsNullOrWhiteSpace(RequestedFilePath))
        {
            return;
        }

        string requestedFilePath = NormalizeRelativePath(
            Uri.UnescapeDataString(RequestedFilePath));

        ProjectStateFile? requestedFile = Files.FirstOrDefault(file =>
            string.Equals(
                NormalizeRelativePath(file.RelativePath),
                requestedFilePath,
                StringComparison.OrdinalIgnoreCase));

        if (requestedFile is null)
        {
            SearchText = requestedFilePath;
            return;
        }

        SearchText = requestedFile.RelativePath;
        SelectedFile = requestedFile;
        SelectedFileContentResult = null;

        LoadSelectedFilePreview();
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath
            .Replace('\\', '/')
            .Trim();
    }
}