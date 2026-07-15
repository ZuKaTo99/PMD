using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectHistory;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectFiles;
using PMD.App.Domain.ProjectStates;
using PMD.App.Features.Projects.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectHistoryPage
{
    protected const string SortByPath = "path";
    protected const string SortByExtension = "extension";
    protected const string SortBySizeDescending = "size-desc";
    protected const string SortByLastChangedDescending = "last-changed-desc";

    [Inject]
    private IProjectHistoryService ProjectHistoryService { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public Guid ProjectStateId { get; set; }

    protected ProjectHistoryDetails? HistoryDetails { get; private set; }

    protected IReadOnlyList<ProjectStateFile> Files { get; private set; } =
        Array.Empty<ProjectStateFile>();

    protected ProjectFileChangeKind? SelectedChangeKind { get; private set; }

    protected ProjectFileChange? SelectedChange { get; private set; }

    protected string ChangeSearchText { get; private set; } = string.Empty;

    protected ProjectStateFile? SelectedFile { get; private set; }

    protected ProjectFileContentResult? SelectedFileContentResult { get; private set; }

    protected string SearchText { get; private set; } = string.Empty;

    protected string SelectedExtension { get; private set; } = string.Empty;

    protected string SelectedSortMode { get; private set; } = SortByPath;

    protected bool HasActiveFileFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        !string.IsNullOrWhiteSpace(SelectedExtension);

    protected string FileListResetKey =>
        $"{ProjectStateId}\u001f{SearchText}\u001f{SelectedExtension}\u001f{SelectedSortMode}";

    protected string SelectedSortLabel => SelectedSortMode switch
    {
        SortByExtension => "Dateityp",
        SortBySizeDescending => "Größe",
        SortByLastChangedDescending => "Änderungsdatum",
        _ => "Pfad"
    };

    protected string PreviousProjectStateDate =>
        HistoryDetails?.PreviousProjectState is ProjectState previousProjectState
            ? ProjectOverviewFormatter.FormatDateTime(previousProjectState.ScannedAt)
            : "Nicht vorhanden";

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

    protected IReadOnlyList<ProjectStateFile> VisibleFiles =>
        SortFiles(FilteredFiles).ToList();

    protected override void OnParametersSet()
    {
        HistoryDetails = ProjectHistoryService.GetDetails(
            ProjectId,
            ProjectStateId);

        SelectedChangeKind = null;
        SelectedChange = null;
        ChangeSearchText = string.Empty;
        SelectedFile = null;
        SelectedFileContentResult = null;
        SearchText = string.Empty;
        SelectedExtension = string.Empty;
        SelectedSortMode = SortByPath;

        Files = HistoryDetails?.SelectedProjectState.Files is { } files
            ? files
            : Array.Empty<ProjectStateFile>();
    }

    protected string BuildProjectStateLink(ProjectState projectState)
    {
        return $"/projekte/{ProjectId}/verlauf/{projectState.Id}";
    }

    protected void OnSelectedChangeKindChanged(
        ProjectFileChangeKind? changeKind)
    {
        SelectedChangeKind = changeKind;

        if (SelectedChange is not null &&
            !MatchesCurrentChangeFilters(SelectedChange))
        {
            SelectedChange = null;
        }
    }

    protected void OnChangeSearchTextChanged(string searchText)
    {
        ChangeSearchText = searchText;

        if (SelectedChange is not null &&
            !MatchesCurrentChangeFilters(SelectedChange))
        {
            SelectedChange = null;
        }
    }

    protected void SelectChange(ProjectFileChange change)
    {
        SelectedChange = change;
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

    protected void ClearFileFilters()
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

    protected void LoadHistoricalFilePreview()
    {
        if (SelectedFile is null)
        {
            SelectedFileContentResult = ProjectFileContentResult.Blocked(
                string.Empty,
                "Es wurde keine Datei ausgewählt.");

            return;
        }

        if (!SelectedFile.HasTextSnapshot)
        {
            SelectedFileContentResult = ProjectFileContentResult.Blocked(
                SelectedFile.RelativePath,
                "Für diese historische Datei wurde kein Textauszug gespeichert.");

            return;
        }

        SelectedFileContentResult = new ProjectFileContentResult
        {
            CanShowContent = true,
            Content = SelectedFile.TextSnapshotContent,
            Message = "Gespeicherter Textauszug aus diesem Projektstand.",
            FullPath = SelectedFile.RelativePath,
            SizeInBytes = SelectedFile.SizeInBytes,
            LineCount = SelectedFile.TextSnapshotLineCount,
            WasTruncated = SelectedFile.TextSnapshotWasTruncated
        };
    }

    private bool MatchesCurrentChangeFilters(ProjectFileChange change)
    {
        if (SelectedChangeKind is null)
        {
            if (change.ChangeKind == ProjectFileChangeKind.Unchanged)
            {
                return false;
            }
        }
        else if (change.ChangeKind != SelectedChangeKind)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(ChangeSearchText))
        {
            return true;
        }

        return change.RelativePath.Contains(
                ChangeSearchText,
                StringComparison.OrdinalIgnoreCase) ||
            change.FileName.Contains(
                ChangeSearchText,
                StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<ProjectStateFile> SortFiles(
        IReadOnlyList<ProjectStateFile> files)
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
}
