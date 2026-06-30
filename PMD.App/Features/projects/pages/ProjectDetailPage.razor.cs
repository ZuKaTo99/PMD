using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Application.Scanner;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.IO;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectDetailPage
{
    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectOverviewService ProjectOverviewService { get; set; } = default!;

    [Inject]
    private IProjectFolderScanner ProjectFolderScanner { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected ProjectOverview? CurrentOverview { get; private set; }

    protected Project? CurrentProject => CurrentOverview?.Project;

    protected IReadOnlyList<ProjectState> CurrentProjectStates =>
        CurrentOverview?.ProjectStates ?? Array.Empty<ProjectState>();

    protected ProjectState? LatestProjectState => CurrentOverview?.LatestProjectState;

    protected ProjectState? PreviousProjectState => CurrentOverview?.PreviousProjectState;

    protected ProjectStateComparisonResult? ChangesSinceLastCheck =>
        CurrentOverview?.ChangesSinceLastCheck;

    protected string? InfoMessage { get; private set; }

    protected string? ErrorMessage { get; private set; }

    protected override void OnParametersSet()
    {
        LoadProjectData();
    }

    protected void ScanCurrentProject()
    {
        InfoMessage = null;
        ErrorMessage = null;

        Project? project = CurrentProject;

        if (project is null)
        {
            ErrorMessage = "Das Projekt wurde nicht gefunden.";
            return;
        }

        if (!Directory.Exists(project.RootPath))
        {
            ErrorMessage = "Der Projektordner wurde nicht gefunden.";
            return;
        }

        try
        {
            var scanResult = ProjectFolderScanner.ScanFolder(project.RootPath);

            Project updatedProject = ProjectMemoryStore.RememberScannedProject(
                scanResult.ProjectName,
                scanResult.RootPath,
                scanResult.ScannedAt);

            ProjectState projectState = ProjectStateBuilder.CreateFromScanResult(
                scanResult.ProjectName,
                scanResult,
                updatedProject.Id);

            ProjectStateMemoryStore.Remember(projectState);

            LoadProjectData();

            InfoMessage = "Projekt wurde erneut geprüft. Der Projektverlauf wurde aktualisiert.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Das Projekt konnte nicht geprüft werden: {ex.Message}";
        }
    }

    private void LoadProjectData()
    {
        CurrentOverview = ProjectOverviewService.GetProjectOverview(ProjectId);
    }
}
