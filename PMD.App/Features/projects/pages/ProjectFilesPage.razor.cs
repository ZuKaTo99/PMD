using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectFilesPage
{
    private const int MaxVisibleFileCount = 100;

    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected Project? CurrentProject { get; private set; }

    protected ProjectState? LatestProjectState { get; private set; }

    protected IReadOnlyList<ProjectStateFile> Files { get; private set; } =
        Array.Empty<ProjectStateFile>();

    protected string SearchText { get; private set; } = string.Empty;

    protected string SelectedExtension { get; private set; } = string.Empty;

    protected bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        !string.IsNullOrWhiteSpace(SelectedExtension);

    protected IReadOnlyList<string> AvailableExtensions => Files
        .Select(file => file.Extension)
        .Where(extension => !string.IsNullOrWhiteSpace(extension))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
        .ToList();

    protected IReadOnlyList<ProjectStateFile> FilteredFiles => Files
        .Where(MatchesSelectedExtension)
        .Where(MatchesSearchText)
        .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
        .ToList();

    protected IReadOnlyList<ProjectStateFile> VisibleFiles => FilteredFiles
        .Take(MaxVisibleFileCount)
        .ToList();

    protected override void OnParametersSet()
    {
        LoadProjectFiles();
    }

    protected void OnSearchTextChanged(ChangeEventArgs eventArgs)
    {
        SearchText = eventArgs.Value?.ToString() ?? string.Empty;
    }

    protected void OnSelectedExtensionChanged(ChangeEventArgs eventArgs)
    {
        SelectedExtension = eventArgs.Value?.ToString() ?? string.Empty;
    }

    protected void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedExtension = string.Empty;
    }

    private void LoadProjectFiles()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
        LatestProjectState = null;
        Files = Array.Empty<ProjectStateFile>();
        SearchText = string.Empty;
        SelectedExtension = string.Empty;

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
}