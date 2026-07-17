using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectHistory;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Features.Projects.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectHistoryComparisonPage
{
    [Inject]
    private IProjectHistoryService ProjectHistoryService { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected IReadOnlyList<ProjectState> ProjectStates { get; private set; } =
        Array.Empty<ProjectState>();

    protected Guid? SelectedOlderProjectStateId { get; private set; }

    protected Guid? SelectedNewerProjectStateId { get; private set; }

    protected ProjectHistoryComparisonDetails? ComparisonDetails { get; private set; }

    protected string ValidationMessage { get; private set; } = string.Empty;

    protected ProjectFileChangeKind? SelectedChangeKind { get; private set; }

    protected ProjectFileChange? SelectedChange { get; private set; }

    protected string ChangeSearchText { get; private set; } = string.Empty;

    protected IReadOnlyList<ProjectState> OlderProjectStateOptions =>
        ProjectStates.Skip(1).ToList();

    protected IReadOnlyList<ProjectState> NewerProjectStateOptions
    {
        get
        {
            int olderProjectStateIndex = FindProjectStateIndex(
                SelectedOlderProjectStateId);

            if (olderProjectStateIndex <= 0)
            {
                return Array.Empty<ProjectState>();
            }

            return ProjectStates
                .Take(olderProjectStateIndex)
                .ToList();
        }
    }

    protected bool CanCompareSelectedStates =>
        SelectedOlderProjectStateId.HasValue &&
        SelectedNewerProjectStateId.HasValue &&
        SelectedOlderProjectStateId != SelectedNewerProjectStateId &&
        NewerProjectStateOptions.Any(
            projectState =>
                projectState.Id == SelectedNewerProjectStateId.Value);

    protected override void OnParametersSet()
    {
        ProjectStates = ProjectHistoryService.GetProjectStates(ProjectId);

        SelectedOlderProjectStateId = null;
        SelectedNewerProjectStateId = null;
        ComparisonDetails = null;
        ValidationMessage = string.Empty;
        ResetChangeSelection();

        if (ProjectStates.Count < 2)
        {
            return;
        }

        SelectedOlderProjectStateId = ProjectStates[1].Id;
        SelectedNewerProjectStateId = ProjectStates[0].Id;
        CompareSelectedStates();
    }

    protected void OnOlderProjectStateChanged(ChangeEventArgs eventArgs)
    {
        if (!Guid.TryParse(
                eventArgs.Value?.ToString(),
                out Guid projectStateId))
        {
            return;
        }

        SelectedOlderProjectStateId = projectStateId;

        IReadOnlyList<ProjectState> newerOptions =
            NewerProjectStateOptions;

        bool selectedNewerStateIsValid =
            SelectedNewerProjectStateId.HasValue &&
            newerOptions.Any(
                projectState =>
                    projectState.Id ==
                    SelectedNewerProjectStateId.Value);

        if (!selectedNewerStateIsValid)
        {
            SelectedNewerProjectStateId =
                newerOptions.LastOrDefault()?.Id;
        }

        ClearCurrentComparison();
    }

    protected void OnNewerProjectStateChanged(ChangeEventArgs eventArgs)
    {
        if (!Guid.TryParse(
                eventArgs.Value?.ToString(),
                out Guid projectStateId))
        {
            return;
        }

        SelectedNewerProjectStateId = projectStateId;
        ClearCurrentComparison();
    }

    protected void CompareSelectedStates()
    {
        ValidationMessage = string.Empty;
        ResetChangeSelection();

        if (!CanCompareSelectedStates ||
            SelectedOlderProjectStateId is not Guid olderProjectStateId ||
            SelectedNewerProjectStateId is not Guid newerProjectStateId)
        {
            ComparisonDetails = null;
            ValidationMessage =
                "Bitte einen älteren Ausgangsstand und einen neueren Zielstand auswählen.";

            return;
        }

        ComparisonDetails = ProjectHistoryService.GetComparisonDetails(
            ProjectId,
            olderProjectStateId,
            newerProjectStateId);

        if (ComparisonDetails is null)
        {
            ValidationMessage =
                "Die ausgewählten Projektstände konnten nicht miteinander verglichen werden.";
        }
    }

    protected string FormatProjectStateOption(ProjectState projectState)
    {
        int projectStateNumber = GetProjectStateNumber(projectState.Id);

        return $"Prüfung {projectStateNumber} – " +
            ProjectOverviewFormatter.FormatDateTime(projectState.ScannedAt);
    }

    protected string BuildProjectStateLink(ProjectState projectState)
    {
        return $"/projekte/{ProjectId}/verlauf/{projectState.Id}";
    }

    protected string FormatSignedFileCount(int difference)
    {
        return difference switch
        {
            > 0 => $"+{difference} Dateien",
            < 0 => $"{difference} Dateien",
            _ => "Unverändert"
        };
    }

    protected string FormatSignedFileSize(long differenceInBytes)
    {
        if (differenceInBytes == 0)
        {
            return "Unverändert";
        }

        string prefix = differenceInBytes > 0 ? "+" : "−";
        string formattedSize = ProjectOverviewFormatter.FormatFileSize(
            Math.Abs(differenceInBytes));

        return $"{prefix}{formattedSize}";
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

    private void ClearCurrentComparison()
    {
        ComparisonDetails = null;
        ValidationMessage = string.Empty;
        ResetChangeSelection();
    }

    private void ResetChangeSelection()
    {
        SelectedChangeKind = null;
        SelectedChange = null;
        ChangeSearchText = string.Empty;
    }

    private int FindProjectStateIndex(Guid? projectStateId)
    {
        if (!projectStateId.HasValue)
        {
            return -1;
        }

        for (int index = 0; index < ProjectStates.Count; index++)
        {
            if (ProjectStates[index].Id == projectStateId.Value)
            {
                return index;
            }
        }

        return -1;
    }

    private int GetProjectStateNumber(Guid projectStateId)
    {
        int projectStateIndex = FindProjectStateIndex(projectStateId);

        return projectStateIndex < 0
            ? 0
            : ProjectStates.Count - projectStateIndex;
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
}
