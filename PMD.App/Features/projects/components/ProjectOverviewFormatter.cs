using System;

namespace PMD.App.Features.Projects.Components;

internal static class ProjectOverviewFormatter
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

    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("dd.MM.yyyy HH:mm");
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1000)
        {
            return $"{duration.TotalMilliseconds:0} ms";
        }

        return $"{duration.TotalSeconds:0.0} Sekunden";
    }
}
