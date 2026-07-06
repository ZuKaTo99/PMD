using PMD.App.Application.ProjectFiles;
using PMD.App.Domain.ProjectFiles;
using System.Text;

namespace PMD.App.Infrastructure.ProjectFiles;

public sealed class ProjectFileContentReader : IProjectFileContentReader
{
    private const long MaxPreviewSizeInBytes = 200 * 1024;
    private const int MaxPreviewLineCount = 400;

    private static readonly HashSet<string> SupportedTextExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".razor",
        ".xaml",
        ".css",
        ".scss",
        ".html",
        ".htm",
        ".js",
        ".ts",
        ".json",
        ".xml",
        ".config",
        ".csproj",
        ".sln",
        ".props",
        ".targets",
        ".md",
        ".txt",
        ".gitignore",
        ".editorconfig",
        ".yml",
        ".yaml"
    };

    public ProjectFileContentResult ReadPreview(
        string projectRootPath,
        string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectRootPath))
        {
            return ProjectFileContentResult.Blocked(
                string.Empty,
                "Der Projektordner ist nicht bekannt.");
        }

        if (string.IsNullOrWhiteSpace(relativeFilePath))
        {
            return ProjectFileContentResult.Blocked(
                string.Empty,
                "Es wurde keine Datei ausgewählt.");
        }

        try
        {
            string fullPath = BuildSafeFullPath(projectRootPath, relativeFilePath);

            if (!IsInsideProjectRoot(projectRootPath, fullPath))
            {
                return ProjectFileContentResult.Blocked(
                    fullPath,
                    "Die Datei liegt nicht im geöffneten Projektordner.");
            }

            if (!File.Exists(fullPath))
            {
                return ProjectFileContentResult.Blocked(
                    fullPath,
                    "Die Datei wurde im Projektordner nicht gefunden.");
            }

            FileInfo fileInfo = new(fullPath);

            if (!IsSupportedTextFile(fileInfo.Name, fileInfo.Extension))
            {
                return ProjectFileContentResult.Blocked(
                    fullPath,
                    "Dieser Dateityp wird für die Vorschau noch nicht unterstützt.");
            }

            if (fileInfo.Length > MaxPreviewSizeInBytes)
            {
                return ProjectFileContentResult.Blocked(
                    fullPath,
                    "Die Datei ist für die Vorschau noch zu groß.");
            }

            return ReadSmallTextFile(fileInfo);
        }
        catch (UnauthorizedAccessException)
        {
            return ProjectFileContentResult.Blocked(
                string.Empty,
                "PMD darf diese Datei nicht lesen.");
        }
        catch (IOException)
        {
            return ProjectFileContentResult.Blocked(
                string.Empty,
                "Die Datei konnte nicht gelesen werden.");
        }
        catch (ArgumentException)
        {
            return ProjectFileContentResult.Blocked(
                string.Empty,
                "Der Dateipfad ist ungültig.");
        }
        catch (NotSupportedException)
        {
            return ProjectFileContentResult.Blocked(
                string.Empty,
                "Der Dateipfad wird nicht unterstützt.");
        }
    }

    private static ProjectFileContentResult ReadSmallTextFile(FileInfo fileInfo)
    {
        List<string> lines = new();
        bool wasTruncated = false;

        using StreamReader reader = new(
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

            if (ContainsBinaryMarker(line))
            {
                return ProjectFileContentResult.Blocked(
                    fileInfo.FullName,
                    "Die Datei wirkt nicht wie eine reine Textdatei.");
            }

            if (lines.Count >= MaxPreviewLineCount)
            {
                wasTruncated = true;
                break;
            }

            lines.Add(line);
        }

        string content = string.Join(Environment.NewLine, lines);

        return ProjectFileContentResult.Success(
            fileInfo.FullName,
            fileInfo.Length,
            content,
            lines.Count,
            wasTruncated);
    }

    private static string BuildSafeFullPath(
        string projectRootPath,
        string relativeFilePath)
    {
        string normalizedRootPath = Path.GetFullPath(projectRootPath);
        string combinedPath = Path.Combine(normalizedRootPath, relativeFilePath);

        return Path.GetFullPath(combinedPath);
    }

    private static bool IsInsideProjectRoot(
        string projectRootPath,
        string fullPath)
    {
        string normalizedRootPath = Path.GetFullPath(projectRootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string normalizedFullPath = Path.GetFullPath(fullPath);

        string rootWithSeparator = normalizedRootPath + Path.DirectorySeparatorChar;

        return normalizedFullPath.Equals(
                normalizedRootPath,
                StringComparison.OrdinalIgnoreCase) ||
            normalizedFullPath.StartsWith(
                rootWithSeparator,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedTextFile(string fileName, string extension)
    {
        if (SupportedTextExtensions.Contains(fileName))
        {
            return true;
        }

        return SupportedTextExtensions.Contains(extension);
    }

    private static bool ContainsBinaryMarker(string line)
    {
        return line.Contains('\0');
    }
}