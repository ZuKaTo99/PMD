using CommunityToolkit.Maui.Storage;
using Microsoft.AspNetCore.Components;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Scanner;
using PMD.App.Domain.ProjectStates;
using PMD.App.Domain.Scanner;
using System.IO;
using System.Linq;
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


    private const int DisplayedFileLimit = 50;
    private const int ComparisonFileLimit = 20;

    private string folderPath = string.Empty;
    private string? errorMessage;
    private string? infoMessage;
    private ProjectFolderScanResult? scanResult;
    private ProjectState? preparedProjectState;
    private IReadOnlyList<ProjectState> RememberedProjectStates => ProjectStateMemoryStore.ProjectStates;
    private ProjectStateComparisonResult? comparisonResult;
    private ProjectState? comparedOldProjectState;
    private ProjectState? comparedNewProjectState;
    private Guid? selectedOldProjectStateId;
    private Guid? selectedNewProjectStateId;


    private void ClearMessages()
    {
        errorMessage = null;
        infoMessage = null;
    }

    private void ClearScanData()
    {
        scanResult = null;
        preparedProjectState = null;
    }

    private void ClearComparisonData()
    {
        comparisonResult = null;
        comparedOldProjectState = null;
        comparedNewProjectState = null;
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
        preparedProjectState = null;
        ClearComparisonData();

        try
        {
            var result = await FolderPicker.PickAsync(CancellationToken.None);

            if (result.IsSuccessful && result.Folder is not null)
            {
                folderPath = result.Folder.Path;
                scanResult = null;
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
        ClearComparisonData();

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

            preparedProjectState = ProjectStateBuilder.CreateFromScanResult(
                scanResult.ProjectName,
                scanResult);

            ShowInfoMessage("Projektprüfung abgeschlossen. Projektstand wurde vorbereitet.");
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Die Projektprüfung konnte nicht abgeschlossen werden: {ex.Message}");
        }
    }

    private void RememberPreparedProjectState()
    {
        if (preparedProjectState is null)
        {
            return;
        }

        bool wasRemembered = ProjectStateMemoryStore.Remember(preparedProjectState);

        if (!wasRemembered)
        {
            infoMessage = "Dieser Projektstand wurde bereits gemerkt.";
            return;
        }

        SelectPreparedProjectStateForComparison();

        ClearComparisonData();
        ShowInfoMessage("Projektstand wurde gemerkt.");
    }

    private void SelectPreparedProjectStateForComparison()
    {
        if (preparedProjectState is null)
        {
            return;
        }

        selectedNewProjectStateId = preparedProjectState.Id;

        selectedOldProjectStateId = RememberedProjectStates
            .Where(projectState => projectState.Id != preparedProjectState.Id)
            .FirstOrDefault(projectState => ProjectStateFolderMatcher.IsSameProjectFolder(projectState, preparedProjectState))
            ?.Id;
    }

    private void ClearRememberedProjectStates()
    {
        ProjectStateMemoryStore.Clear();

        selectedOldProjectStateId = null;
        selectedNewProjectStateId = null;

        ClearComparisonData();
        ShowInfoMessage("Gemerkte Stände wurden geleert.");
    }

    private void CompareLatestProjectStates()
    {
        if (RememberedProjectStates.Count < 2)
        {
            return;
        }

        var newState = RememberedProjectStates[0];
        var oldState = RememberedProjectStates[1];

        if (!ProjectStateFolderMatcher.IsSameProjectFolder(oldState, newState))
        {
            ClearComparisonData();
            ShowErrorMessage("Es können nur Projektstände aus demselben Projektordner verglichen werden.");
            return;
        }

        comparedOldProjectState = oldState;
        comparedNewProjectState = newState;
        comparisonResult = ProjectStateComparer.Compare(oldState, newState);
        ShowInfoMessage("Vergleich wurde erstellt.");
    }

    private ProjectState? GetSelectedOldProjectState()
    {
        if (selectedOldProjectStateId is null)
        {
            return null;
        }

        return RememberedProjectStates
            .FirstOrDefault(projectState => projectState.Id == selectedOldProjectStateId);
    }

    private ProjectState? GetSelectedNewProjectState()
    {
        if (selectedNewProjectStateId is null)
        {
            return null;
        }

        return RememberedProjectStates
            .FirstOrDefault(projectState => projectState.Id == selectedNewProjectStateId);
    }

    private void CompareSelectedProjectStates()
    {
        ProjectState? oldState = GetSelectedOldProjectState();
        ProjectState? newState = GetSelectedNewProjectState();

        if (oldState is null || newState is null)
        {
            ClearComparisonData();
            ShowErrorMessage("Bitte wählen Sie zuerst einen alten und einen neuen Projektstand aus.");
            return;
        }

        if (oldState.Id == newState.Id)
        {
            ClearComparisonData();
            ShowErrorMessage("Bitte wählen Sie zwei unterschiedliche Projektstände aus.");
            return;
        }

        if (!ProjectStateFolderMatcher.IsSameProjectFolder(oldState, newState))
        {
            ClearComparisonData();
            ShowErrorMessage("Es können nur Projektstände aus demselben Projektordner verglichen werden.");
            return;
        }

        comparedOldProjectState = oldState;
        comparedNewProjectState = newState;
        comparisonResult = ProjectStateComparer.Compare(oldState, newState);
        ShowInfoMessage("Vergleich wurde erstellt.");
    }

    private bool IsPreparedProjectStateRemembered()
    {
        if (preparedProjectState is null)
        {
            return false;
        }

        return RememberedProjectStates
            .Any(projectState => projectState.Id == preparedProjectState.Id);
    }

    private int GetDisplayedFileCount()
    {
        if (scanResult is null)
        {
            return 0;
        }

        return Math.Min(scanResult.FileCount, DisplayedFileLimit);
    }

}