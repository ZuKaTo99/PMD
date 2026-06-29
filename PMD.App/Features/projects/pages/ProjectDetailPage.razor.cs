using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectDetailPage
{
    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected Project? CurrentProject { get; private set; }

    protected IReadOnlyList<ProjectState> CurrentProjectStates { get; private set; } =
        Array.Empty<ProjectState>();

    protected override void OnParametersSet()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
        CurrentProjectStates = GetCurrentProjectStates();
    }

    private IReadOnlyList<ProjectState> GetCurrentProjectStates()
    {
        if (CurrentProject is null)
        {
            return Array.Empty<ProjectState>();
        }

        return ProjectStateMemoryStore.ProjectStates
            .Where(projectState => ProjectStateFolderMatcher.IsSameProjectFolder(
                projectState,
                CurrentProject.RootPath))
            .ToList();
    }
}