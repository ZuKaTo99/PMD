using PMD.App.Application.ProjectStates;
using PMD.App.Domain.ProjectStates;

namespace PMD.App.Features.Scanner.Pages;

public partial class ProjectScannerPage
{
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