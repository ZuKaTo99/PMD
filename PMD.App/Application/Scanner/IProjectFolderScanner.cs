using PMD.App.Domain.Scanner;

namespace PMD.App.Application.Scanner;

public interface IProjectFolderScanner
{
    ProjectFolderScanResult ScanFolder(string folderPath);

    Task<ProjectFolderScanResult> ScanFolderAsync(
        string folderPath,
        IProgress<ProjectFolderScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}