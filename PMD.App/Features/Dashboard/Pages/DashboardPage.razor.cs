using Microsoft.AspNetCore.Components;
using PMD.App.Application.Analytics;
using PMD.App.Application.Dashboard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PMD.App.Features.Dashboard.Pages;

public partial class DashboardPage : IDisposable
{
    private const int MaximumDashboardLanguageCount = 8;
    private const int MaximumProjectLanguageCount = 5;

    [Inject]
    private IDashboardOverviewService DashboardOverviewService { get; set; } =
        default!;

    protected DashboardOverview Overview { get; private set; } = new();

    protected IReadOnlyList<DashboardProjectActivity> ComparableActivities =>
        Overview.ProjectActivities
            .Where(activity => activity.HasComparison)
            .OrderByDescending(activity => activity.TotalChangeCount)
            .ThenBy(activity => activity.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    protected IReadOnlyList<ProjectLanguageUsage> DisplayedLanguages =>
        CreateDisplayedLanguages(
            Overview.LanguageUsage,
            MaximumDashboardLanguageCount);

    protected override void OnInitialized()
    {
        DashboardOverviewService.OverviewChanged += OnOverviewChanged;
        RefreshOverview();
    }

    public void Dispose()
    {
        DashboardOverviewService.OverviewChanged -= OnOverviewChanged;
    }

    protected string GetChartHeightStyle(
        DashboardProjectActivity activity)
    {
        if (activity.TotalChangeCount <= 0 ||
            Overview.MaxProjectChangeCount <= 0)
        {
            return "height: 0%;";
        }

        double relativeHeight =
            activity.TotalChangeCount /
            (double)Overview.MaxProjectChangeCount * 100d;

        double visibleHeight = Math.Max(8d, relativeHeight);

        return $"height: {visibleHeight.ToString("0.#", CultureInfo.InvariantCulture)}%;";
    }

    protected static string GetSegmentStyle(int count)
    {
        return $"flex-grow: {Math.Max(0, count)};";
    }

    protected static string GetLanguageSegmentStyle(
        ProjectLanguageUsage language)
    {
        string percentage = language.Percentage.ToString(
            "0.####",
            CultureInfo.InvariantCulture);

        return $"--dashboard-language-color: {language.Color}; " +
            $"width: {percentage}%;";
    }

    protected static string GetLanguageColorStyle(string color)
    {
        return $"--dashboard-language-color: {color};";
    }


    protected static string GetSparklinePoints(
        IReadOnlyList<int> values)
    {
        const double width = 120d;
        const double height = 36d;
        const double padding = 3d;

        if (values.Count == 0)
        {
            return $"{padding.ToString(CultureInfo.InvariantCulture)}," +
                $"{(height / 2d).ToString(CultureInfo.InvariantCulture)} " +
                $"{(width - padding).ToString(CultureInfo.InvariantCulture)}," +
                $"{(height / 2d).ToString(CultureInfo.InvariantCulture)}";
        }

        IReadOnlyList<int> normalizedValues = values.Count == 1
            ? new[] { values[0], values[0] }
            : values;

        int minimum = normalizedValues.Min();
        int maximum = normalizedValues.Max();
        double range = Math.Max(1d, maximum - minimum);
        double xStep = (width - (padding * 2d)) /
            Math.Max(1, normalizedValues.Count - 1);

        return string.Join(
            " ",
            normalizedValues.Select((value, index) =>
            {
                double x = padding + (index * xStep);
                double relativeValue = (value - minimum) / range;
                double y = height - padding -
                    (relativeValue * (height - (padding * 2d)));

                if (minimum == maximum)
                {
                    y = height / 2d;
                }

                return $"{x.ToString("0.##", CultureInfo.InvariantCulture)}," +
                    $"{y.ToString("0.##", CultureInfo.InvariantCulture)}";
            }));
    }

    protected static string GetProjectTrendAriaLabel(
        DashboardProjectActivity activity)
    {
        if (activity.ProjectStateCount <= 1)
        {
            return $"{activity.ProjectName}: erster gespeicherter Projektstand mit " +
                $"{activity.LatestFileCount:N0} Dateien.";
        }

        int firstFileCount = activity.FileCountHistory.Count > 0
            ? activity.FileCountHistory[0]
            : activity.LatestFileCount;

        int difference = activity.LatestFileCount - firstFileCount;
        string development = difference switch
        {
            > 0 => $"um {difference:N0} Dateien gewachsen",
            < 0 => $"um {Math.Abs(difference):N0} Dateien kleiner geworden",
            _ => "bei der Dateianzahl stabil geblieben"
        };

        return $"{activity.ProjectName}: in den letzten " +
            $"{activity.ProjectStateCount:N0} gespeicherten Projektständen " +
            $"{development}.";
    }

    protected static string FormatLanguageDetails(
        ProjectLanguageUsage language)
    {
        return $"{language.FileCount:N0} Dateien · {FormatBytes(language.SizeInBytes)}";
    }

    protected static IReadOnlyList<ProjectLanguageUsage>
        GetDisplayedProjectLanguages(DashboardProjectActivity activity)
    {
        return CreateDisplayedLanguages(
            activity.LanguageUsage,
            MaximumProjectLanguageCount);
    }

    protected static string GetLanguageDistributionAriaLabel(
        IReadOnlyList<ProjectLanguageUsage> languages)
    {
        if (languages.Count == 0)
        {
            return "Keine unterstützte Programmiersprache erkannt.";
        }

        return "Sprachverteilung: " + string.Join(
            ", ",
            languages.Select(language =>
                $"{language.Name} {FormatPercentage(language.Percentage)}"));
    }

    protected static string GetLanguageTitle(ProjectLanguageUsage language)
    {
        return $"{language.Name}: {FormatPercentage(language.Percentage)}";
    }

    protected static string GetActivityAriaLabel(
        DashboardProjectActivity activity)
    {
        return $"{activity.ProjectName}: " +
            $"{activity.AddedFileCount} neue, " +
            $"{activity.ModifiedFileCount} geänderte und " +
            $"{activity.RemovedFileCount} entfernte Dateien.";
    }

    protected static string FormatProjectLabel(int projectCount)
    {
        return projectCount == 1
            ? "gespeichertes Projekt"
            : "gespeicherte Projekte";
    }

    protected static string FormatComparisonLabel(int comparisonCount)
    {
        return comparisonCount == 1
            ? "Projekt mit Vergleich"
            : "Projekte mit Vergleich";
    }

    protected static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("dd.MM.yyyy HH:mm");
    }

    protected static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1000)
        {
            return $"{duration.TotalMilliseconds:0} ms";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:0.0} s";
        }

        return $"{(int)duration.TotalMinutes}:{duration.Seconds:00} min";
    }

    protected static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        int unitIndex = 0;

        while (value >= 1024d && unitIndex < units.Length - 1)
        {
            value /= 1024d;
            unitIndex++;
        }

        string format = unitIndex == 0 || value >= 100d
            ? "0"
            : value >= 10d
                ? "0.0"
                : "0.00";

        return $"{value.ToString(format, CultureInfo.CurrentCulture)} {units[unitIndex]}";
    }

    protected static string FormatPercentage(double percentage)
    {
        if (percentage > 0d && percentage < 0.1d)
        {
            return "<0,1 %";
        }

        return $"{percentage:0.0} %";
    }

    protected static char GetProjectInitial(string projectName)
    {
        string trimmedName = projectName?.Trim() ?? string.Empty;

        return trimmedName.Length == 0
            ? 'P'
            : char.ToUpperInvariant(trimmedName[0]);
    }

    protected static string FormatFileDelta(int fileCountDifference)
    {
        return fileCountDifference switch
        {
            > 0 => $"+{fileCountDifference:N0} Dateien",
            < 0 => $"{fileCountDifference:N0} Dateien",
            _ => "Dateizahl stabil"
        };
    }

    protected static string GetFileDeltaClass(int fileCountDifference)
    {
        return fileCountDifference switch
        {
            > 0 => "dashboard-file-delta-positive",
            < 0 => "dashboard-file-delta-negative",
            _ => "dashboard-file-delta-neutral"
        };
    }

    private static IReadOnlyList<ProjectLanguageUsage> CreateDisplayedLanguages(
        IReadOnlyList<ProjectLanguageUsage> languages,
        int maximumVisibleCount)
    {
        if (languages.Count <= maximumVisibleCount)
        {
            return languages;
        }

        int namedLanguageCount = Math.Max(1, maximumVisibleCount - 1);
        List<ProjectLanguageUsage> displayedLanguages = languages
            .Take(namedLanguageCount)
            .ToList();

        IReadOnlyList<ProjectLanguageUsage> remainingLanguages = languages
            .Skip(namedLanguageCount)
            .ToList();

        displayedLanguages.Add(new ProjectLanguageUsage
        {
            Name = "Weitere",
            Color = "#8b949e",
            SizeInBytes = remainingLanguages.Sum(language => language.SizeInBytes),
            FileCount = remainingLanguages.Sum(language => language.FileCount),
            Percentage = remainingLanguages.Sum(language => language.Percentage)
        });

        return displayedLanguages;
    }

    private void OnOverviewChanged()
    {
        _ = InvokeAsync(() =>
        {
            RefreshOverview();
            StateHasChanged();
        });
    }

    private void RefreshOverview()
    {
        Overview = DashboardOverviewService.GetOverview();
    }
}
