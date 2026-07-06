namespace PMD.App.Domain.ProjectFiles;

public sealed class ProjectFileContentResult
{
    public bool CanShowContent { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;

    public long SizeInBytes { get; init; }

    public int LineCount { get; init; }

    public bool WasTruncated { get; init; }

    public static ProjectFileContentResult Success(
        string fullPath,
        long sizeInBytes,
        string content,
        int lineCount,
        bool wasTruncated)
    {
        return new ProjectFileContentResult
        {
            CanShowContent = true,
            FullPath = fullPath,
            SizeInBytes = sizeInBytes,
            Content = content,
            LineCount = lineCount,
            WasTruncated = wasTruncated,
            Message = wasTruncated
                ? "Die Datei wurde für die Vorschau gekürzt."
                : "Die Datei kann angezeigt werden."
        };
    }

    public static ProjectFileContentResult Blocked(string fullPath, string message)
    {
        return new ProjectFileContentResult
        {
            CanShowContent = false,
            FullPath = fullPath,
            Message = message
        };
    }
}