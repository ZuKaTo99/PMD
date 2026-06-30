using System;

namespace PMD.App.Application.Projects;

public interface IProjectOverviewService
{
    ProjectOverview? GetProjectOverview(Guid projectId);
}
