using PMD.App.Application.Scanner;
using PMD.App.Domain.Scanner;

namespace PMD.App.Infrastructure.Scanner;

public sealed class ProjectFolderScanner : IProjectFolderScanner
{
    private static readonly HashSet<string> IgnoredFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".vs",
        ".git",
        "node_modules",
        "Library",
        "Temp"
    };

    public ProjectFolderScanResult ScanFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Der Projektordner darf nicht leer sein.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Der Projektordner wurde nicht gefunden: {folderPath}");
        }

        var rootPath = Path.GetFullPath(folderPath);
        var files = new List<ProjectFileEntry>();

        foreach (var filePath in EnumerateFilesSafe(rootPath))
        {
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                continue;
            }

            files.Add(new ProjectFileEntry
            {
                FullPath = fileInfo.FullName,
                RelativePath = Path.GetRelativePath(rootPath, fileInfo.FullName),
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension,
                SizeInBytes = fileInfo.Length,
                LastChangedAt = fileInfo.LastWriteTime
            });
        }

        return new ProjectFolderScanResult
        {
            RootPath = rootPath,
            ScannedAt = DateTime.Now,
            Files = files
        };
    }

    private static IEnumerable<string> EnumerateFilesSafe(string rootPath)
    {
        var foldersToCheck = new Stack<string>();
        foldersToCheck.Push(rootPath);

        while (foldersToCheck.Count > 0)
        {
            var currentFolder = foldersToCheck.Pop();

            string[] subFolders;
            string[] files;

            try
            {
                subFolders = Directory.GetDirectories(currentFolder);
                files = Directory.GetFiles(currentFolder);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var subFolder in subFolders)
            {
                var folderName = Path.GetFileName(subFolder);

                if (IgnoredFolderNames.Contains(folderName))
                {
                    continue;
                }

                foldersToCheck.Push(subFolder);
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }
}