using System;

namespace PMD.App.Features.Home.Components;

internal static class HomeDisplayFormatter
{
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

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:0.0} Sekunden";
        }

        return $"{(int)duration.TotalMinutes}:{duration.Seconds:00} Minuten";
    }

    public static string FormatFileCount(int fileCount)
    {
        return fileCount == 1
            ? "1 Datei"
            : $"{fileCount:N0} Dateien";
    }

    public static string FormatProjectLabel(int projectCount)
    {
        return projectCount == 1
            ? "gespeichertes Projekt"
            : "gespeicherte Projekte";
    }
}
