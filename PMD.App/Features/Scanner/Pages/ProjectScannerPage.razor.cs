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

public partial class ProjectScannerPage
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
    private ProjectFolderScanResult? scanResult;
    private Project? currentProject;

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
                ShowErrorMessage($"Der Ordner konnte nicht ausgewählt werden: {result.Exception.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Der Ordner konnte nicht ausgewählt werden: {ex.Message}");
        }
    }

    private void ScanProjectFolder()
    {
        ClearMessages();
        ClearScanData();

        folderPath = folderPath.Trim();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            ShowErrorMessage("Bitte geben Sie zuerst einen Projektordner an.");
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            ShowErrorMessage("Der angegebene Projektordner wurde nicht gefunden.");
            return;
        }

        try
        {
            scanResult = ProjectFolderScanner.ScanFolder(folderPath);

            currentProject = ProjectMemoryStore.RememberScannedProject(
                scanResult.ProjectName,
                scanResult.RootPath,
                scanResult.ScannedAt);

            ProjectState projectState = ProjectStateBuilder.CreateFromScanResult(
                currentProject.Name,
                scanResult,
                currentProject.Id);

            ProjectStateMemoryStore.Remember(projectState);

            ShowInfoMessage("Projektprüfung abgeschlossen. Das Projekt wurde aufgenommen und der Verlauf wurde aktualisiert.");
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Die Projektprüfung konnte nicht abgeschlossen werden: {ex.Message}");
        }
    }
}
