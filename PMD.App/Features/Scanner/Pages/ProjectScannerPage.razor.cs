using CommunityToolkit.Maui.Storage;
using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Application.Scanner;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Projects;
using PMD.App.Domain.Scanner;
using System.IO;
using System.Threading;

namespace PMD.App.Features.Scanner.Pages;

public partial class ProjectScannerPage : IDisposable
{
    [Inject]
    private IProjectFolderScanner ProjectFolderScanner { get; set; } = default!;

    [Inject]
    private IFolderPicker FolderPicker { get; set; } = default!;

    [Inject]
    private IProjectStateMemoryStore ProjectStateMemoryStore { get; set; } = default!;

    [Inject]
    private IProjectMemoryStore ProjectMemoryStore { get; set; } = default!;

    private string folderPath = string.Empty;
    private string? errorMessage;
    private string? infoMessage;
    private string scanStatusText = string.Empty;

    private ProjectFolderScanResult? scanResult;
    private ProjectFolderScanProgress? scanProgress;
    private Project? currentProject;

    private CancellationTokenSource? scanCancellationTokenSource;

    private bool isScanOperationActive;
    private bool isSavingProjectState;
    private bool isCancellationRequested;

    private bool CanCancelScan =>
        isScanOperationActive &&
        !isSavingProjectState &&
        !isCancellationRequested &&
        scanCancellationTokenSource is not null;

    private void ClearMessages()
    {
        errorMessage = null;
        infoMessage = null;
    }

    private void ClearScanData()
    {
        scanResult = null;
        currentProject = null;
    }

    private void ShowInfoMessage(string message)
    {
        errorMessage = null;
        infoMessage = message;
    }

    private void ShowErrorMessage(string message)
    {
        infoMessage = null;
        errorMessage = message;
    }

    private async Task PickFolderAsync()
    {
        if (isScanOperationActive)
        {
            return;
        }

        ClearMessages();
        ClearScanData();

        try
        {
            var result = await FolderPicker.PickAsync(CancellationToken.None);

            if (result.IsSuccessful && result.Folder is not null)
            {
                folderPath = result.Folder.Path;
                ShowInfoMessage("Ordner wurde ausgewählt.");
            }
            else if (result.Exception is not null)
            {
                ShowErrorMessage(
                    $"Der Ordner konnte nicht ausgewählt werden: {result.Exception.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage(
                $"Der Ordner konnte nicht ausgewählt werden: {ex.Message}");
        }
    }

    private async Task ScanProjectFolderAsync()
    {
        if (isScanOperationActive)
        {
            return;
        }

        ClearMessages();
        ClearScanData();

        folderPath = folderPath.Trim();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ShowErrorMessage(
                "Bitte geben Sie zuerst einen Projektordner an.");

            return;
        }

        if (!Directory.Exists(folderPath))
        {
            ShowErrorMessage(
                "Der angegebene Projektordner wurde nicht gefunden.");

            return;
        }

        using var cancellationTokenSource = new CancellationTokenSource();

        scanCancellationTokenSource = cancellationTokenSource;
        isScanOperationActive = true;
        isSavingProjectState = false;
        isCancellationRequested = false;
        scanProgress = null;
        scanStatusText = "Projekt wird geprüft";

        var progress = new Progress<ProjectFolderScanProgress>(
            OnScanProgressChanged);

        try
        {
            ProjectFolderScanResult completedScanResult =
                await ProjectFolderScanner.ScanFolderAsync(
                    folderPath,
                    progress,
                    cancellationTokenSource.Token);

            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            scanResult = completedScanResult;
            isSavingProjectState = true;
            scanStatusText = "Projektstand wird gespeichert";

            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            currentProject = ProjectMemoryStore.RememberScannedProject(
                scanResult.ProjectName,
                scanResult.RootPath,
                scanResult.ScannedAt);

            ProjectState projectState =
                ProjectStateBuilder.CreateFromScanResult(
                    currentProject.Name,
                    scanResult,
                    currentProject.Id);

            ProjectStateMemoryStore.Remember(projectState);

            ShowInfoMessage(
                "Projektprüfung abgeschlossen. Das Projekt wurde aufgenommen und der Verlauf wurde aktualisiert.");
        }
        catch (OperationCanceledException)
        {
            ClearScanData();

            ShowInfoMessage(
                "Die Projektprüfung wurde abgebrochen. Es wurde kein neuer Projektstand gespeichert.");
        }
        catch (Exception ex)
        {
            ClearScanData();

            ShowErrorMessage(
                $"Die Projektprüfung konnte nicht abgeschlossen werden: {ex.Message}");
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