using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;
using System.IO;
using System.Linq;
using System.Threading;

namespace PMD.App.Features.Scanner.Pages;

public partial class ProjectScannerPage
{
    private async Task PickFolderAsync()
    {
        errorMessage = null;
        infoMessage = null;
        preparedProjectState = null;
        comparisonResult = null;
        comparedOldProjectState = null;
        comparedNewProjectState = null;

        try
        {
            var result = await FolderPicker.PickAsync(CancellationToken.None);

            if (result.IsSuccessful && result.Folder is not null)
            {
                folderPath = result.Folder.Path;
                scanResult = null;
                infoMessage = "Ordner wurde ausgewählt.";
            }
            else if (result.Exception is not null)
            {
                errorMessage = $"Der Ordner konnte nicht ausgewählt werden: {result.Exception.Message}";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Der Ordner konnte nicht ausgewählt werden: {ex.Message}";
        }
    }

    private void ScanProjectFolder()
    {
        errorMessage = null;
        infoMessage = null;
        scanResult = null;
        preparedProjectState = null;
        comparisonResult = null;
        comparedOldProjectState = null;
        comparedNewProjectState = null;
        folderPath = folderPath.Trim();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            errorMessage = "Bitte geben Sie zuerst einen Projektordner an.";
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            errorMessage = "Der angegebene Projektordner wurde nicht gefunden.";
            return;
        }

        try
        {
            scanResult = ProjectFolderScanner.ScanFolder(folderPath);

            preparedProjectState = ProjectStateBuilder.CreateFromScanResult(
                scanResult.ProjectName,
                scanResult);

            infoMessage = "Projektprüfung abgeschlossen. Projektstand wurde vorbereitet.";
        }
        catch (Exception ex)
        {
            errorMessage = $"Die Projektprüfung konnte nicht abgeschlossen werden: {ex.Message}";
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

        comparisonResult = null;
        comparedOldProjectState = null;
        comparedNewProjectState = null;
        errorMessage = null;
        infoMessage = "Projektstand wurde gemerkt.";
    }

    private void ClearRememberedProjectStates()
    {
        ProjectStateMemoryStore.Clear();

        comparisonResult = null;
        comparedOldProjectState = null;
        comparedNewProjectState = null;
        errorMessage = null;
        infoMessage = "Gemerkte Stände wurden geleert.";
    }

    private void CompareLatestProjectStates()
    {
        if (RememberedProjectStates.Count < 2)
        {
            return;
        }

        var newState = RememberedProjectStates[0];
        var oldState = RememberedProjectStates[1];

        if (!IsSameProjectFolder(oldState, newState))
        {
            comparisonResult = null;
            infoMessage = null;
            errorMessage = "Es können nur Projektstände aus demselben Projektordner verglichen werden.";
            return;
        }

        errorMessage = null;
        comparedOldProjectState = oldState;
        comparedNewProjectState = newState;
        comparisonResult = ProjectStateComparer.Compare(oldState, newState);
        infoMessage = "Vergleich wurde erstellt.";
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

    private static bool IsSameProjectFolder(
        ProjectState oldState,
        ProjectState newState)
    {
        string oldPath = NormalizeFolderPath(oldState.RootPath);
        string newPath = NormalizeFolderPath(newState.RootPath);

        return string.Equals(
            oldPath,
            newPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        return folderPath
            .Trim()
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}