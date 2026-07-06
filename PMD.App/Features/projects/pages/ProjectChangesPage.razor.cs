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

    protected ProjectState? LatestProjectState => ProjectStates.FirstOrDefault();

    protected ProjectState? PreviousProjectState => ProjectStates.Skip(1).FirstOrDefault();

    protected override void OnParametersSet()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
        ChangesResult = null;

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

    protected static string FormatProjectStateDate(ProjectState? projectState)
    {
        if (projectState is null)
        {
            return "Nicht vorhanden";
        }

        return ProjectOverviewFormatter.FormatDateTime(projectState.ScannedAt);
    }
}