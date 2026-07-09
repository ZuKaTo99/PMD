using System;
using System.Collections.Generic;

namespace PMD.App.Domain.Projects;

public static class ProjectAccentColors
{
    public const string Blue = "blue";
    public const string Violet = "violet";
    public const string Cyan = "cyan";
    public const string Emerald = "emerald";
    public const string Amber = "amber";
    public const string Rose = "rose";

    public const string Default = Blue;

    public static IReadOnlyList<string> All { get; } =
    [
        Blue,
        Violet,
        Cyan,
        Emerald,
        Amber,
        Rose
    ];

    public static string Normalize(string? accentColor)
    {
        if (string.IsNullOrWhiteSpace(accentColor))
        {
            return Default;
        }

        return accentColor.Trim().ToLowerInvariant() switch
        {
            Blue => Blue,
            Violet => Violet,
            Cyan => Cyan,
            Emerald => Emerald,
            Amber => Amber,
            Rose => Rose,
            _ => Default
        };
    }

    public static bool IsKnown(string? accentColor)
    {
        if (string.IsNullOrWhiteSpace(accentColor))
        {
            return false;
        }

        string normalizedAccentColor = Normalize(accentColor);
        string normalizedInput = accentColor.Trim().ToLowerInvariant();

        return string.Equals(
            normalizedAccentColor,
            normalizedInput,
            StringComparison.Ordinal);
    }
}