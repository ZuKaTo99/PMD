using Microsoft.Maui.Storage;
using PMD.App.Application.Home;
using PMD.App.Domain.Home;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Infrastructure.Home;

public sealed class HomeWidgetPreferencesService
    : IHomeWidgetPreferencesService
{
    private const string HiddenWidgetsPreferenceKey =
        "pmd-home-hidden-widgets";

    private const string WidgetOrderPreferenceKey =
        "pmd-home-widget-order";

    private const string WidgetSizesPreferenceKey =
        "pmd-home-widget-sizes";

    private static readonly IReadOnlyList<HomeWidgetId> DefaultWidgetOrder =
    [
        HomeWidgetId.ProjectOverview,
        HomeWidgetId.QuickActions,
        HomeWidgetId.RecentProjects,
        HomeWidgetId.RecentChecks,
        HomeWidgetId.LanguageUsage
    ];

    private static readonly IReadOnlyDictionary<HomeWidgetId, HomeWidgetSize>
        DefaultWidgetSizes =
            new Dictionary<HomeWidgetId, HomeWidgetSize>
            {
                [HomeWidgetId.ProjectOverview] = HomeWidgetSize.Compact,
                [HomeWidgetId.QuickActions] = HomeWidgetSize.Wide,
                [HomeWidgetId.RecentProjects] = HomeWidgetSize.Wide,
                [HomeWidgetId.RecentChecks] = HomeWidgetSize.Compact,
                [HomeWidgetId.LanguageUsage] = HomeWidgetSize.Full
            };

    private readonly HashSet<HomeWidgetId> hiddenWidgets;
    private readonly List<HomeWidgetId> widgetOrder;
    private readonly Dictionary<HomeWidgetId, HomeWidgetSize> widgetSizes;

    public HomeWidgetPreferencesService()
    {
        hiddenWidgets = LoadHiddenWidgets();
        widgetOrder = LoadWidgetOrder();
        widgetSizes = LoadWidgetSizes();
    }

    public event Action? PreferencesChanged;

    public bool IsVisible(HomeWidgetId widgetId)
    {
        return !hiddenWidgets.Contains(widgetId);
    }

    public HomeWidgetSize GetSize(HomeWidgetId widgetId)
    {
        return widgetSizes.TryGetValue(widgetId, out HomeWidgetSize size)
            ? size
            : GetDefaultSize(widgetId);
    }

    public IReadOnlyList<HomeWidgetId> GetWidgetOrder()
    {
        return widgetOrder.ToList();
    }

    public void SetVisibility(
        HomeWidgetId widgetId,
        bool isVisible)
    {
        bool changed = isVisible
            ? hiddenWidgets.Remove(widgetId)
            : hiddenWidgets.Add(widgetId);

        if (!changed)
        {
            return;
        }

        SaveHiddenWidgets();
        PreferencesChanged?.Invoke();
    }

    public void SetSize(
        HomeWidgetId widgetId,
        HomeWidgetSize size)
    {
        if (!Enum.IsDefined(typeof(HomeWidgetId), widgetId) ||
            !Enum.IsDefined(typeof(HomeWidgetSize), size) ||
            GetSize(widgetId) == size)
        {
            return;
        }

        widgetSizes[widgetId] = size;
        SaveWidgetSizes();
        PreferencesChanged?.Invoke();
    }

    public void MoveTo(
        HomeWidgetId widgetId,
        HomeWidgetId targetWidgetId)
    {
        int currentIndex = widgetOrder.IndexOf(widgetId);
        int targetIndex = widgetOrder.IndexOf(targetWidgetId);

        if (currentIndex < 0 ||
            targetIndex < 0 ||
            currentIndex == targetIndex)
        {
            return;
        }

        widgetOrder.RemoveAt(currentIndex);
        widgetOrder.Insert(
            Math.Min(targetIndex, widgetOrder.Count),
            widgetId);

        SaveWidgetOrder();
        PreferencesChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        bool visibilityChanged = hiddenWidgets.Count > 0;
        bool orderChanged = !widgetOrder.SequenceEqual(DefaultWidgetOrder);
        bool sizesChanged = DefaultWidgetSizes.Any(
            entry => GetSize(entry.Key) != entry.Value);

        if (!visibilityChanged && !orderChanged && !sizesChanged)
        {
            return;
        }

        hiddenWidgets.Clear();
        widgetOrder.Clear();
        widgetOrder.AddRange(DefaultWidgetOrder);

        widgetSizes.Clear();
        foreach (KeyValuePair<HomeWidgetId, HomeWidgetSize> entry
                 in DefaultWidgetSizes)
        {
            widgetSizes[entry.Key] = entry.Value;
        }

        Preferences.Default.Remove(HiddenWidgetsPreferenceKey);
        Preferences.Default.Remove(WidgetOrderPreferenceKey);
        Preferences.Default.Remove(WidgetSizesPreferenceKey);
        PreferencesChanged?.Invoke();
    }

    private void SaveHiddenWidgets()
    {
        string value = string.Join(
            ",",
            hiddenWidgets
                .OrderBy(widgetId => widgetId)
                .Select(widgetId => widgetId.ToString()));

        Preferences.Default.Set(
            HiddenWidgetsPreferenceKey,
            value);
    }

    private void SaveWidgetOrder()
    {
        string value = string.Join(
            ",",
            widgetOrder.Select(widgetId => widgetId.ToString()));

        Preferences.Default.Set(
            WidgetOrderPreferenceKey,
            value);
    }

    private void SaveWidgetSizes()
    {
        string value = string.Join(
            ",",
            widgetOrder.Select(
                widgetId => $"{widgetId}:{GetSize(widgetId)}"));

        Preferences.Default.Set(
            WidgetSizesPreferenceKey,
            value);
    }

    private static HashSet<HomeWidgetId> LoadHiddenWidgets()
    {
        string value = Preferences.Default.Get(
            HiddenWidgetsPreferenceKey,
            string.Empty);

        HashSet<HomeWidgetId> widgets = new();

        foreach (string entry in SplitPreferenceValue(value))
        {
            if (TryParseWidgetId(entry, out HomeWidgetId widgetId))
            {
                widgets.Add(widgetId);
            }
        }

        return widgets;
    }

    private static List<HomeWidgetId> LoadWidgetOrder()
    {
        string value = Preferences.Default.Get(
            WidgetOrderPreferenceKey,
            string.Empty);

        List<HomeWidgetId> order = new();

        foreach (string entry in SplitPreferenceValue(value))
        {
            if (TryParseWidgetId(entry, out HomeWidgetId widgetId) &&
                !order.Contains(widgetId))
            {
                order.Add(widgetId);
            }
        }

        foreach (HomeWidgetId widgetId in DefaultWidgetOrder)
        {
            if (!order.Contains(widgetId))
            {
                order.Add(widgetId);
            }
        }

        return order;
    }

    private static Dictionary<HomeWidgetId, HomeWidgetSize> LoadWidgetSizes()
    {
        Dictionary<HomeWidgetId, HomeWidgetSize> sizes =
            DefaultWidgetSizes.ToDictionary(
                entry => entry.Key,
                entry => entry.Value);

        string value = Preferences.Default.Get(
            WidgetSizesPreferenceKey,
            string.Empty);

        foreach (string entry in SplitPreferenceValue(value))
        {
            string[] parts = entry.Split(
                ':',
                2,
                StringSplitOptions.TrimEntries);

            if (parts.Length != 2 ||
                !TryParseWidgetId(parts[0], out HomeWidgetId widgetId) ||
                !Enum.TryParse(
                    parts[1],
                    ignoreCase: true,
                    out HomeWidgetSize size) ||
                !Enum.IsDefined(typeof(HomeWidgetSize), size))
            {
                continue;
            }

            sizes[widgetId] = size;
        }

        return sizes;
    }

    private static bool TryParseWidgetId(
        string value,
        out HomeWidgetId widgetId)
    {
        return Enum.TryParse(
                value,
                ignoreCase: true,
                out widgetId) &&
            Enum.IsDefined(typeof(HomeWidgetId), widgetId);
    }

    private static IEnumerable<string> SplitPreferenceValue(string value)
    {
        return value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }

    private static HomeWidgetSize GetDefaultSize(HomeWidgetId widgetId)
    {
        return DefaultWidgetSizes.TryGetValue(
            widgetId,
            out HomeWidgetSize size)
                ? size
                : HomeWidgetSize.Standard;
    }

}
