using PMD.App.Domain.ProjectStates;
using System;
using System.Collections.Generic;

namespace PMD.App.Features.Scanner.Components;

public static class ProjectStateDisplayHelper
{
    public static int GetProjectStateNumber(
        IReadOnlyList<ProjectState> projectStates,
        ProjectState? projectState)
    {
        ArgumentNullException.ThrowIfNull(projectStates);

        if (projectState is null)
        {
            return 0;
        }

        for (var index = 0; index < projectStates.Count; index++)
        {
            if (projectStates[index].Id == projectState.Id)
            {
                return projectStates.Count - index;
            }
        }

        return 0;
    }

    public static string GetProjectStateLabel(
        IReadOnlyList<ProjectState> projectStates,
        ProjectState? projectState)
    {
        int projectStateNumber = GetProjectStateNumber(projectStates, projectState);

        return ScannerDisplayFormatter.FormatProjectStateLabel(projectStateNumber);
    }
}