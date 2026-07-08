using PMD.App.Application.Scanner;
using PMD.App.Domain.Scanner;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PMD.App.Infrastructure.Scanner;

public sealed class ProjectFolderScanner : IProjectFolderScanner
{
    private const long MaxTextSnapshotSizeInBytes = 200 * 1024;
    private const int MaxTextSnapshotLineCount = 400;

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

        string rootPath = Path.GetFullPath(folderPath);
        var files = new List<ProjectFileEntry>();
        var ignoredFolders = new List<string>();
        var warnings = new List<string>();
        var scannedFolderCount = 0;

        foreach (string filePath in EnumerateFilesSafe(
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

                string relativePath = Path.GetRelativePath(rootPath, fileInfo.FullName);

                string contentHashSha256 = TryComputeFileHashSha256(
                    fileInfo.FullName,
                    relativePath,
                    warnings);

                ProjectTextSnapshot textSnapshot = TryCreateTextSnapshot(
                    fileInfo,
                    relativePath,
                    warnings);

                files.Add(new ProjectFileEntry
                {
                    FullPath = fileInfo.FullName,
                    RelativePath = relativePath,
                    FileName = fileInfo.Name,
                    Extension = fileInfo.Extension,
                    SizeInBytes = fileInfo.Length,
                    LastChangedAt = fileInfo.LastWriteTime,
                    ContentHashSha256 = contentHashSha256,
                    TextSnapshotContent = textSnapshot.Content,
                    TextSnapshotLineCount = textSnapshot.LineCount,
                    TextSnapshotWasTruncated = textSnapshot.WasTruncated
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
            string currentFolder = foldersToCheck.Pop();
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

            foreach (string subFolder in subFolders)
            {
                string folderName = Path.GetFileName(subFolder);

                if (IgnoredFolderNames.Contains(folderName))
                {
                    ignoredFolders.Add(Path.GetRelativePath(rootPath, subFolder));
                    continue;
                }

                foldersToCheck.Push(subFolder);
            }
            
            foreach (string file in files)
            {
                yield return file;
            }
        }
    }

    private static string TryComputeFileHashSha256(
        string filePath,
        string relativePath,
        List<string> warnings)
    {
        try
        {
            using FileStream fileStream = File.OpenRead(filePath);
            byte[] hashBytes = SHA256.HashData(fileStream);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (UnauthorizedAccessException)
        {
            warnings.Add($"Dateiinhalt konnte nicht geprüft werden: {relativePath}");
            return string.Empty;
        }
        catch (IOException)
        {
            warnings.Add($"Dateiinhalt konnte nicht geprüft werden: {relativePath}");
            return string.Empty;
        }
    }

    private static ProjectTextSnapshot TryCreateTextSnapshot(
        FileInfo fileInfo,
        string relativePath,
        List<string> warnings)
    {
        if (!IsSupportedTextSnapshotFile(fileInfo.Name, fileInfo.Extension))
        {
            return ProjectTextSnapshot.Empty;
        }

        if (fileInfo.Length > MaxTextSnapshotSizeInBytes)
        {
            return ProjectTextSnapshot.Empty;
        }

        try
        {
            var lines = new List<string>();
            bool wasTruncated = false;

            using var reader = new StreamReader(
                fileInfo.FullName,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();

                if (line is null)
                {
                    break;
                }

                if (line.Contains('\0'))
                {
                    return ProjectTextSnapshot.Empty;
                }

                if (lines.Count >= MaxTextSnapshotLineCount)
                {
                    wasTruncated = true;
                    break;
                }

                lines.Add(line);
            }

            return new ProjectTextSnapshot(
                string.Join(Environment.NewLine, lines),
                lines.Count,
                wasTruncated);
        }
        catch (UnauthorizedAccessException)
        {
            warnings.Add($"Textinhalt konnte nicht gespeichert werden: {relativePath}");
            return ProjectTextSnapshot.Empty;
        }
        catch (IOException)
        {
            warnings.Add($"Textinhalt konnte nicht gespeichert werden: {relativePath}");
            return ProjectTextSnapshot.Empty;
        }
    }

    private static bool IsSupportedTextSnapshotFile(string fileName, string extension)
    {
        return ProjectTextFileRules.IsSupportedTextSnapshotFile(fileName, extension);
    }

    private sealed record ProjectTextSnapshot(
        string Content,
        int LineCount,
        bool WasTruncated)
    {
        public static ProjectTextSnapshot Empty { get; } = new(
            string.Empty,
            0,
            false);
    }
}