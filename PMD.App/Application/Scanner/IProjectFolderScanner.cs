using PMD.App.Domain.Scanner;

namespace PMD.App.Application.Scanner;

public interface IProjectFolderScanner
{
    ProjectFolderScanResult ScanFolder(string folderPath);
}