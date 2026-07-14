using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Application.Scanner;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using PMD.App.Domain.Scanner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PMD.App.Features.Projects.Pages;

public partial class ProjectDetailPage : IDisposable
{
    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectOverviewService ProjectOverviewService { get; set; } = default!;

    [Inject]
    private IProjectFolderScanner ProjectFolderScanner { get; set; } = default!;

    [Inject]
    private IProjectFolderLauncher ProjectFolderLauncher { get; set; } = default!;

    [Parameter]
    public Guid ProjectId { get; set; }

    protected ProjectOverview? CurrentOverview { get; private set; }

    protected Project? CurrentProject => CurrentOverview?.Project;

    protected IReadOnlyList<ProjectState> CurrentProjectStates =>
        CurrentOverview?.ProjectStates ?? Array.Empty<ProjectState>();

    protected ProjectState? LatestProjectState =>
        CurrentOverview?.LatestProjectState;

    protected ProjectState? PreviousProjectState =>
        CurrentOverview?.PreviousProjectState;

    protected ProjectStateComparisonResult? ChangesSinceLastCheck =>
        CurrentOverview?.ChangesSinceLastCheck;

    protected string? InfoMessage { get; private set; }

    protected string? ErrorMessage { get; private set; }

    private CancellationTokenSource? scanCancellationTokenSource;

    private ProjectFolderScanProgress? scanProgress;

    private string scanStatusText = string.Empty;

    private bool isScanOperationActive;

    private bool isSavingProjectState;

    private bool isCancellationRequested;

    private bool CanCancelScan =>
        isScanOperationActive &&
        !isSavingProjectState &&
        !isCancellationRequested &&
        scanCancellationTokenSource is not null;

    protected override void OnParametersSet()
    {
        LoadProjectData();
    }

    private async Task ScanCurrentProjectAsync()
    {
        if (isScanOperationActive)
        {
            return;
        }

        ClearMessages();

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

        using var cancellationTokenSource = new CancellationTokenSource();

        scanCancellationTokenSource = cancellationTokenSource;
        isScanOperationActive = true;
        isSavingProjectState = false;
        isCancellationRequested = false;
        scanProgress = null;
        scanStatusText = "Projekt wird erneut geprüft";

        var progress = new Progress<ProjectFolderScanProgress>(
            OnScanProgressChanged);

        try
        {
            ProjectFolderScanResult scanResult =
                await ProjectFolderScanner.ScanFolderAsync(
                    project.RootPath,
                    progress,
                    cancellationTokenSource.Token);

            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            isSavingProjectState = true;
            scanStatusText = "Projektstand wird gespeichert";

            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            Project updatedProject =
                ProjectMemoryStore.RememberScannedProject(
                    scanResult.ProjectName,
                    scanResult.RootPath,
                    scanResult.ScannedAt);

            ProjectState projectState =
                ProjectStateBuilder.CreateFromScanResult(
                    updatedProject.Name,
                    scanResult,
                    updatedProject.Id);

            ProjectStateMemoryStore.Remember(projectState);

            LoadProjectData();

            InfoMessage =
                "Projekt wurde erneut geprüft. Der Projektverlauf wurde aktualisiert.";
        }
        catch (OperationCanceledException)
        {
            InfoMessage =
                "Die Projektprüfung wurde abgebrochen. Es wurde kein neuer Projektstand gespeichert.";
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Das Projekt konnte nicht geprüft werden: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(
                    scanCancellationTokenSource,
                    cancellationTokenSource))
            {
                scanCancellationTokenSource = null;
            }

            isScanOperationActive = false;
            isSavingProjectState = false;
            isCancellationRequested = false;
            scanProgress = null;
            scanStatusText = string.Empty;
        }
    }

    private void OnScanProgressChanged(
        ProjectFolderScanProgress progress)
    {
        if (!isScanOperationActive)
        {
            return;
        }

        scanProgress = progress;

        _ = InvokeAsync(StateHasChanged);
    }

    private void CancelScan()
    {
        if (!CanCancelScan)
        {
            return;
        }

        CancellationTokenSource? cancellationTokenSource =
            scanCancellationTokenSource;

        if (cancellationTokenSource is null)
        {
            return;
        }

        isCancellationRequested = true;
        scanStatusText = "Projektprüfung wird abgebrochen";

        cancellationTokenSource.Cancel();
    }

    private void OpenCurrentProjectFolder()
    {
        ClearMessages();

        Project? project = CurrentProject;

        if (project is null)
        {
            ErrorMessage = "Das Projekt wurde nicht gefunden.";
            return;
        }

        try
        {
            ProjectFolderLauncher.OpenFolder(project.RootPath);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Der Projektordner konnte nicht geöffnet werden: {ex.Message}";
        }
    }

    private void ClearMessages()
    {
        InfoMessage = null;
        ErrorMessage = null;
    }

    private void LoadProjectData()
    {
        CurrentOverview =
            ProjectOverviewService.GetProjectOverview(ProjectId);
    }

    public void Dispose()
    {
        CancellationTokenSource? cancellationTokenSource =
            scanCancellationTokenSource;

        if (cancellationTokenSource is null ||
            cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        cancellationTokenSource.Cancel();
    }
}