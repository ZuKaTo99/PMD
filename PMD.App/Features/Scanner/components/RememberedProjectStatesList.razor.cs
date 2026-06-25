using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMD.App.Features.Scanner.Components;

public partial class RememberedProjectStatesList
{
    [Parameter]
    public IReadOnlyList<ProjectState> ProjectStates { get; set; } = Array.Empty<ProjectState>();

    [Parameter]
    public Guid? SelectedOldProjectStateId { get; set; }

    [Parameter]
    public EventCallback<Guid?> SelectedOldProjectStateIdChanged { get; set; }

    [Parameter]
    public Guid? SelectedNewProjectStateId { get; set; }

    [Parameter]
    public EventCallback<Guid?> SelectedNewProjectStateIdChanged { get; set; }

    [Parameter]
    public EventCallback OnCompareLatestProjectStates { get; set; }

    [Parameter]
    public EventCallback OnCompareSelectedProjectStates { get; set; }

    [Parameter]
    public EventCallback OnClearRememberedProjectStates { get; set; }

    private bool CanCompareSelectedProjectStates
    {
        get
        {
            ProjectState? oldState = GetProjectStateById(SelectedOldProjectStateId);
            ProjectState? newState = GetProjectStateById(SelectedNewProjectStateId);

            return oldState is not null
                && newState is not null
                && oldState.Id != newState.Id
                && ProjectStateFolderMatcher.IsSameProjectFolder(oldState, newState);
        }
    }

    private bool HasSelectedSameProjectState =>
        SelectedOldProjectStateId is not null
        && SelectedNewProjectStateId is not null
        && SelectedOldProjectStateId == SelectedNewProjectStateId;

    private bool HasSelectedDifferentProjectFolders
    {
        get
        {
            ProjectState? oldState = GetProjectStateById(SelectedOldProjectStateId);
            ProjectState? newState = GetProjectStateById(SelectedNewProjectStateId);

            return oldState is not null
                && newState is not null
                && oldState.Id != newState.Id
                && !ProjectStateFolderMatcher.IsSameProjectFolder(oldState, newState);
        }
    }

    private async Task OnSelectedOldProjectStateChanged(ChangeEventArgs args)
    {
        SelectedOldProjectStateId = ParseProjectStateId(args.Value?.ToString());

        await SelectedOldProjectStateIdChanged.InvokeAsync(SelectedOldProjectStateId);
    }

    private async Task OnSelectedNewProjectStateChanged(ChangeEventArgs args)
    {
        SelectedNewProjectStateId = ParseProjectStateId(args.Value?.ToString());

        await SelectedNewProjectStateIdChanged.InvokeAsync(SelectedNewProjectStateId);
    }

    private ProjectState? GetProjectStateById(Guid? projectStateId)
    {
        if (projectStateId is null)
        {
            return null;
        }

        return ProjectStates
            .FirstOrDefault(projectState => projectState.Id == projectStateId);
    }

    private static Guid? ParseProjectStateId(string? value)
    {
        if (Guid.TryParse(value, out var projectStateId))
        {
            return projectStateId;
        }

        return null;
    }

    private static string FormatProjectStateId(Guid? projectStateId)
    {
        return projectStateId?.ToString() ?? string.Empty;
    }

    private int GetProjectStateNumber(ProjectState projectState)
    {
        for (var index = 0; index < ProjectStates.Count; index++)
        {
            if (ProjectStates[index].Id == projectState.Id)
            {
                return ProjectStates.Count - index;
            }
        }

        return 0;
    }

    private string BuildProjectStateOptionText(ProjectState projectState)
    {
        int projectStateNumber = GetProjectStateNumber(projectState);

        return ScannerDisplayFormatter.FormatProjectStateOptionText(
            projectStateNumber,
            projectState);
    }
}