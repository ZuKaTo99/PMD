using PMD.App.Domain.ProjectStates;
using System;

namespace PMD.App.Features.Scanner.Components;

public static class ScannerDisplayFormatter
{
    public static string FormatFileSize(long sizeInBytes)
    {
        if (sizeInBytes < 1024)
        {
            return $"{sizeInBytes} B";
        }

        double sizeInKb = sizeInBytes / 1024d;

        if (sizeInKb < 1024)
        {
            return $"{sizeInKb:0.0} KB";
        }

        double sizeInMb = sizeInKb / 1024d;

        if (sizeInMb < 1024)
        {
            return $"{sizeInMb:0.0} MB";
        }

        double sizeInGb = sizeInMb / 1024d;

        return $"{sizeInGb:0.0} GB";
    }

    public static string FormatScanDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1000)
        {
            return $"{duration.TotalMilliseconds:0} ms";
        }

        return $"{duration.TotalSeconds:0.0} Sekunden";
    }

    public static string FormatExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "-";
        }

        return extension;
    }

    public static string FormatProjectStateLabel(int projectStateNumber)
    {
        if (projectStateNumber <= 0)
        {
            return "Stand -";
        }

        return $"Stand {projectStateNumber}";
    }

    public static string FormatProjectStateOptionText(
        int projectStateNumber,
        ProjectState projectState)
    {
        ArgumentNullException.ThrowIfNull(projectState);

        string projectStateLabel = FormatProjectStateLabel(projectStateNumber);

        return $"{projectStateLabel} - {projectState.ProjectName} - {projectState.CreatedAt:dd.MM.yyyy HH:mm} - {projectState.FileCount} Dateien";
    }
}