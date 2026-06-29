using Microsoft.AspNetCore.Components;
using PMD.App.Application.Projects;
using PMD.App.Domain.Projects;
using System;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectDetailPage
{
    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected Project? CurrentProject { get; private set; }

    protected override void OnParametersSet()
    {
        CurrentProject = ProjectMemoryStore.GetProjectById(ProjectId);
    }
}