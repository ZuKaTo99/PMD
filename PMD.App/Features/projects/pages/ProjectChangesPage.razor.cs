using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectChanges;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectChanges;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using PMD.App.Features.Projects.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectChangesPage
{
    private const int LoadedProjectStateCount = 2;

    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectChangesService ProjectChangesService { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected Project? CurrentProject { get; private set; }

    protected IReadOnlyList<ProjectState> ProjectStates { get; private set; } =
        Array.Empty<ProjectState>();

    protected ProjectChangesResult? ChangesResult { get; private set; }

    protected ProjectFileChangeKind? SelectedChangeKind { get; private set; }

    protected ProjectFileChange? SelectedChange { get; private set; }

    protected string ChangeSearchText { get; private set; } = string.Empty;

    protected ProjectState? LatestProjectState => ProjectStates.FirstOrDefault();

    protected ProjectState? PreviousProjectState => ProjectStates.Skip(1).FirstOrDefault();

    protected override void OnParametersSet()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
        ChangesResult = null;
        SelectedChangeKind = null;
        SelectedChange = null;
        ChangeSearchText = string.Empty;

        if (CurrentProject is null)
        {
            ProjectStates = Array.Empty<ProjectState>();
            return;
        }

        ProjectStates = ProjectStateMemoryStore.GetByProjectId(
            ProjectId,
            LoadedProjectStateCount);

        if (PreviousProjectState is not null && LatestProjectState is not null)
        {
            ChangesResult = ProjectChangesService.Compare(
                PreviousProjectState,
                LatestProjectState);
        }
    }

    protected void OnSelectedChangeKindChanged(ProjectFileChangeKind? changeKind)
    {
        SelectedChangeKind = changeKind;

        if (SelectedChange is not null && !MatchesSelectedChangeKind(SelectedChange))
        {
            SelectedChange = null;
        }
    }

    protected void OnChangeSearchTextChanged(string searchText)
    {
        ChangeSearchText = searchText;

        if (SelectedChange is not null && !MatchesCurrentFilters(SelectedChange))
        {
            SelectedChange = null;
        }
    }

    protected void SelectChange(ProjectFileChange change)
    {
        SelectedChange = change;
    }

    private bool MatchesSelectedChangeKind(ProjectFileChange change)
    {
        if (SelectedChangeKind is null)
        {
            return change.ChangeKind != ProjectFileChangeKind.Unchanged;
        }

        return change.ChangeKind == SelectedChangeKind;
    }

    private bool MatchesCurrentFilters(ProjectFileChange change)
    {
        if (!MatchesSelectedChangeKind(change))
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

    protected static string FormatProjectStateDate(ProjectState? projectState)
    {
        if (projectState is null)
        {
            return "Nicht vorhanden";
        }

        return ProjectOverviewFormatter.FormatDateTime(projectState.ScannedAt);
    }
}