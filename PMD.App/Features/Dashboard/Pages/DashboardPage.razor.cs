using Microsoft.AspNetCore.Components;
using PMD.App.Application.Dashboard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PMD.App.Features.Dashboard.Pages;

public partial class DashboardPage : IDisposable
{
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
