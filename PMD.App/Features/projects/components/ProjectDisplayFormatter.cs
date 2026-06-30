using System;

namespace PMD.App.Features.Projects.Components;

internal static class ProjectDisplayFormatter
{
    public static string FormatFileSize(long sizeInBytes)
    {
        if (sizeInBytes < 1024)
        {
            return $"{sizeInBytes} B";
        }

        if (sizeInBytes < 1024 * 1024)
        {
            return $"{sizeInBytes / 1024d:0.0} KB";
        }

        if (sizeInBytes < 1024 * 1024 * 1024)
        {
            return $"{sizeInBytes / 1024d / 1024d:0.0} MB";
        }

        return $"{sizeInBytes / 1024d / 1024d / 1024d:0.0} GB";
    }

    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("dd.MM.yyyy HH:mm");
    }

    public static string FormatExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "ohne Typ";
        }

        return extension;
    }
}