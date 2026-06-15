using System.Diagnostics;
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

        var stopwatch = Stopwatch.StartNew();

        var rootPath = Path.GetFullPath(folderPath);
        var files = new List<ProjectFileEntry>();
        var ignoredFolders = new List<string>();
        var warnings = new List<string>();
        var scannedFolderCount = 0;

        foreach (var filePath in EnumerateFilesSafe(
            rootPath,
            ignoredFolders,
            warnings,
            () => scannedFolderCount++))
        {
            try
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
            catch (UnauthorizedAccessException)
            {
                warnings.Add($"Datei konnte nicht gelesen werden: {Path.GetRelativePath(rootPath, filePath)}");
            }
            catch (IOException)
            {
                warnings.Add($"Datei konnte nicht gelesen werden: {Path.GetRelativePath(rootPath, filePath)}");
            }
        }

        stopwatch.Stop();


        return new ProjectFolderScanResult
        {
            ProjectName = new DirectoryInfo(rootPath).Name,
            RootPath = rootPath,
            ScannedAt = DateTime.Now,
            ScanDuration = stopwatch.Elapsed,
            ScannedFolderCount = scannedFolderCount,
            Files = files
        .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
        .ToList(),
            IgnoredFolders = ignoredFolders
        .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase)
        .ToList(),
            Warnings = warnings
        .OrderBy(warning => warning, StringComparer.OrdinalIgnoreCase)
        .ToList()
        };


    }

    private static IEnumerable<string> EnumerateFilesSafe(
        string rootPath,
        List<string> ignoredFolders,
        List<string> warnings,
        Action countScannedFolder)
    {
        var foldersToCheck = new Stack<string>();
        foldersToCheck.Push(rootPath);

        while (foldersToCheck.Count > 0)
        {
            var currentFolder = foldersToCheck.Pop();
            countScannedFolder();

            string[] subFolders;
            string[] files;

            try
            {
                subFolders = Directory.GetDirectories(currentFolder);
                files = Directory.GetFiles(currentFolder);
            }
            catch (UnauthorizedAccessException)
            {
                warnings.Add($"Ordner konnte nicht gelesen werden: {Path.GetRelativePath(rootPath, currentFolder)}");
                continue;
            }
            catch (IOException)
            {
                warnings.Add($"Ordner konnte nicht gelesen werden: {Path.GetRelativePath(rootPath, currentFolder)}");
                continue;
            }

            foreach (var subFolder in subFolders)
            {
                var folderName = Path.GetFileName(subFolder);

                if (IgnoredFolderNames.Contains(folderName))
                {
                    ignoredFolders.Add(Path.GetRelativePath(rootPath, subFolder));
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