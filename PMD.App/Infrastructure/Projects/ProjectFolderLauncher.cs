using PMD.App.Application.Projects;
using System.Diagnostics;

namespace PMD.App.Infrastructure.Projects;

public sealed class ProjectFolderLauncher : IProjectFolderLauncher
{
    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException(
                "Der Projektordner wurde nicht angegeben.",
                nameof(folderPath));
        }

        string normalizedFolderPath = Path.GetFullPath(folderPath);

        if (!Directory.Exists(normalizedFolderPath))
        {
            throw new DirectoryNotFoundException(
                "Der Projektordner wurde nicht gefunden.");
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Das Öffnen im Explorer wird derzeit nur unter Windows unterstützt.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(normalizedFolderPath);
        Process.Start(startInfo);
    }
}
