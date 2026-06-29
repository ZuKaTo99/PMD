using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Application.Scanner;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectDetailPage
{
    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectFolderScanner ProjectFolderScanner { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected Project? CurrentProject { get; private set; }

    protected IReadOnlyList<ProjectState> CurrentProjectStates { get; private set; } =
        Array.Empty<ProjectState>();

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

        if (CurrentProject is null)
        {
            ErrorMessage = "Das Projekt wurde nicht gefunden.";
            return;
        }

        if (!Directory.Exists(CurrentProject.RootPath))
        {
            ErrorMessage = "Der Projektordner wurde nicht gefunden.";
            return;
        }

        try
        {
            var scanResult = ProjectFolderScanner.ScanFolder(CurrentProject.RootPath);

            CurrentProject = ProjectMemoryStore.RememberScannedProject(
                scanResult.ProjectName,
                scanResult.RootPath,
                scanResult.ScannedAt);

            ProjectState projectState = ProjectStateBuilder.CreateFromScanResult(
                scanResult.ProjectName,
                scanResult);

            ProjectStateMemoryStore.Remember(projectState);

            LoadProjectData();

            InfoMessage = "Projekt wurde erneut geprüft. Die Prüfung wurde im Projektverlauf gespeichert.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Das Projekt konnte nicht geprüft werden: {ex.Message}";
        }
    }

    private void LoadProjectData()
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