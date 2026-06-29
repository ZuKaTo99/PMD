using Microsoft.AspNetCore.Components;
using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Features.Scanner.Components;

public partial class RememberedProjectStatesList
{
    [Parameter]
    public IReadOnlyList<ProjectState> ProjectStates { get; set; } = Array.Empty<ProjectState>();

    [Parameter]
    public EventCallback OnClearRememberedProjectStates { get; set; }

    protected static string BuildProjectStateTitle(
        int projectStateNumber,
        ProjectState projectState)
    {
        string projectStateLabel = ScannerDisplayFormatter.FormatProjectStateLabel(projectStateNumber);

        return $"{projectStateLabel} · {projectState.ProjectName}";
    }
}