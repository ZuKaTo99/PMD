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
    private const int ProgressReportIntervalInMilliseconds = 100;
    private const int FileStreamBufferSize = 81920;

    private static readonly HashSet<string> IgnoredFolderNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".vs",
        ".git",
        "node_modules",
        "Library",
        "Temp"
    };

    private readonly SemaphoreSlim scanSemaphore = new(1, 1);

    public ProjectFolderScanResult ScanFolder(string folderPath)
    {
        return ScanFolderAsync(
                folderPath,
                progress: null,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<ProjectFolderScanResult> ScanFolderAsync(
        string folderPath,
        IProgress<ProjectFolderScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        bool scanStarted = await scanSemaphore
            .WaitAsync(0, cancellationToken)
            .ConfigureAwait(false);

        if (!scanStarted)
        {
            throw new InvalidOperationException(
                "Es läuft bereits eine Projektprüfung.");
        }

        try
        {
            return await Task.Run(
                    () => ScanFolderCoreAsync(
                        folderPath,
                        progress,
                        cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            scanSemaphore.Release();
        }
    }

    private static async Task<ProjectFolderScanResult> ScanFolderCoreAsync(
        string folderPath,
        IProgress<ProjectFolderScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        ValidateFolderPath(folderPath);

        string rootPath = Path.GetFullPath(folderPath);

        var scanStopwatch = Stopwatch.StartNew();
        var progressStopwatch = Stopwatch.StartNew();

        var files = new List<ProjectFileEntry>();
        var ignoredFolders = new List<string>();
        var warnings = new List<string>();
        var foldersToCheck = new Stack<string>();

        int foundFileCount = 0;
        int processedFileCount = 0;
        int scannedFolderCount = 0;

        string currentFolder = GetRelativeDisplayPath(rootPath, rootPath);
        string currentFile = string.Empty;

        foldersToCheck.Push(rootPath);

        ReportProgress(force: true);

        while (foldersToCheck.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentFolderPath = foldersToCheck.Pop();

            currentFolder = GetRelativeDisplayPath(
                rootPath,
                currentFolderPath);

            currentFile = string.Empty;
            scannedFolderCount++;

            ReportProgress(force: true);

            string[] subFolders;
            string[] folderFiles;

            try
            {
                subFolders = Directory.GetDirectories(currentFolderPath);
                folderFiles = Directory.GetFiles(currentFolderPath);
            }
            catch (UnauthorizedAccessException)
            {
                warnings.Add(
                    $"Ordner konnte nicht gelesen werden: {currentFolder}");

                continue;
            }
            catch (IOException)
            {
                warnings.Add(
                    $"Ordner konnte nicht gelesen werden: {currentFolder}");

                continue;
            }

            foreach (string subFolder in subFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string folderName = Path.GetFileName(subFolder);

                if (IgnoredFolderNames.Contains(folderName))
                {
                    ignoredFolders.Add(
                        Path.GetRelativePath(rootPath, subFolder));

                    continue;
                }

                foldersToCheck.Push(subFolder);
            }

            foundFileCount += folderFiles.Length;

            ReportProgress(force: true);

            foreach (string filePath in folderFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                currentFile = Path.GetRelativePath(rootPath, filePath);

                ReportProgress();

                try
                {
                    var fileInfo = new FileInfo(filePath);

                    if (fileInfo.Exists)
                    {
                        string contentHashSha256 =
                            await TryComputeFileHashSha256Async(
                                    fileInfo.FullName,
                                    currentFile,
                                    warnings,
                                    cancellationToken)
                                .ConfigureAwait(false);

                        ProjectTextSnapshot textSnapshot =
                            await TryCreateTextSnapshotAsync(
                                    fileInfo,
                                    currentFile,
                                    warnings,
                                    cancellationToken)
                                .ConfigureAwait(false);

                        files.Add(new ProjectFileEntry
                        {
                            FullPath = fileInfo.FullName,
                            RelativePath = currentFile,
                            FileName = fileInfo.Name,
                            Extension = fileInfo.Extension,
                            SizeInBytes = fileInfo.Length,
                            LastChangedAt = fileInfo.LastWriteTime,
                            ContentHashSha256 = contentHashSha256,
                            TextSnapshotContent = textSnapshot.Content,
                            TextSnapshotLineCount = textSnapshot.LineCount,
                            TextSnapshotWasTruncated =
                                textSnapshot.WasTruncated
                        });
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    warnings.Add(
                        $"Datei konnte nicht gelesen werden: {currentFile}");
                }
                catch (IOException)
                {
                    warnings.Add(
                        $"Datei konnte nicht gelesen werden: {currentFile}");
                }

                processedFileCount++;

                ReportProgress();
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        scanStopwatch.Stop();

        currentFile = string.Empty;

        ReportProgress(force: true);

        return new ProjectFolderScanResult
        {
            ProjectName = new DirectoryInfo(rootPath).Name,
            RootPath = rootPath,
            ScannedAt = DateTime.Now,
            ScanDuration = scanStopwatch.Elapsed,
            ScannedFolderCount = scannedFolderCount,
            Files = files
                .OrderBy(
                    file => file.RelativePath,
                    StringComparer.OrdinalIgnoreCase)
                .ToList(),
            IgnoredFolders = ignoredFolders
                .OrderBy(
                    folder => folder,
                    StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Warnings = warnings
                .OrderBy(
                    warning => warning,
                    StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        void ReportProgress(bool force = false)
        {
            if (progress is null)
            {
                return;
            }

            if (!force &&
                progressStopwatch.ElapsedMilliseconds <
                ProgressReportIntervalInMilliseconds)
            {
                return;
            }

            progress.Report(new ProjectFolderScanProgress(
                foundFileCount,
                processedFileCount,
                scannedFolderCount,
                ignoredFolders.Count,
                currentFolder,
                currentFile));

            progressStopwatch.Restart();
        }
    }

    private static void ValidateFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException(
                "Der Projektordner darf nicht leer sein.",
                nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"Der Projektordner wurde nicht gefunden: {folderPath}");
        }
    }

    private static async Task<string> TryComputeFileHashSha256Async(
        string filePath,
        string relativePath,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileStreamBufferSize,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

            byte[] hashBytes = await SHA256
                .HashDataAsync(fileStream, cancellationToken)
                .ConfigureAwait(false);

            return Convert
                .ToHexString(hashBytes)
                .ToLowerInvariant();
        }
        catch (UnauthorizedAccessException)
        {
            warnings.Add(
                $"Dateiinhalt konnte nicht geprüft werden: {relativePath}");

            return string.Empty;
        }
        catch (IOException)
        {
            warnings.Add(
                $"Dateiinhalt konnte nicht geprüft werden: {relativePath}");

            return string.Empty;
        }
    }

    private static async Task<ProjectTextSnapshot>
        TryCreateTextSnapshotAsync(
            FileInfo fileInfo,
            string relativePath,
            List<string> warnings,
            CancellationToken cancellationToken)
    {
        if (!IsSupportedTextSnapshotFile(
                fileInfo.Name,
                fileInfo.Extension))
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

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? line = await reader
                    .ReadLineAsync(cancellationToken)
                    .ConfigureAwait(false);

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
            warnings.Add(
                $"Textinhalt konnte nicht gespeichert werden: {relativePath}");

            return ProjectTextSnapshot.Empty;
        }
        catch (IOException)
        {
            warnings.Add(
                $"Textinhalt konnte nicht gespeichert werden: {relativePath}");

            return ProjectTextSnapshot.Empty;
        }
    }

    private static string GetRelativeDisplayPath(
        string rootPath,
        string path)
    {
        string relativePath = Path.GetRelativePath(rootPath, path);

        return relativePath == "."
            ? "Projektstamm"
            : relativePath;
    }

    private static bool IsSupportedTextSnapshotFile(
        string fileName,
        string extension)
    {
        return ProjectTextFileRules.IsSupportedTextSnapshotFile(
            fileName,
            extension);
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