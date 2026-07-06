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
    private const int PreviewFileCount = 12;

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

    protected IReadOnlyList<ProjectStateFile> PreviewFiles => Files
        .OrderBy(file => file.RelativePath)
        .Take(PreviewFileCount)
        .ToList();

    protected override void OnParametersSet()
    {
        LoadProjectFiles();
    }

    private void LoadProjectFiles()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
        LatestProjectState = null;
        Files = Array.Empty<ProjectStateFile>();

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
}