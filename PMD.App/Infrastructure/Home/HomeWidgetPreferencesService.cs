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

    private static readonly IReadOnlyList<HomeWidgetId> DefaultWidgetOrder =
    [
        HomeWidgetId.ProjectOverview,
        HomeWidgetId.QuickActions,
        HomeWidgetId.RecentProjects,
        HomeWidgetId.RecentChecks
    ];

    private readonly HashSet<HomeWidgetId> hiddenWidgets;
    private readonly List<HomeWidgetId> widgetOrder;

    public HomeWidgetPreferencesService()
    {
        hiddenWidgets = LoadHiddenWidgets();
        widgetOrder = LoadWidgetOrder();
    }

    public event Action? PreferencesChanged;

    public bool IsVisible(HomeWidgetId widgetId)
    {
        return !hiddenWidgets.Contains(widgetId);
    }

    public IReadOnlyList<HomeWidgetId> GetWidgetOrder()
    {
        return widgetOrder.ToList();
    }

    public bool CanMoveEarlier(HomeWidgetId widgetId)
    {
        return FindSiblingIndex(widgetId, searchEarlier: true) >= 0;
    }

    public bool CanMoveLater(HomeWidgetId widgetId)
    {
        return FindSiblingIndex(widgetId, searchEarlier: false) >= 0;
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

    public void MoveEarlier(HomeWidgetId widgetId)
    {
        MoveWidget(widgetId, searchEarlier: true);
    }

    public void MoveLater(HomeWidgetId widgetId)
    {
        MoveWidget(widgetId, searchEarlier: false);
    }

    public void ResetToDefaults()
    {
        bool visibilityChanged = hiddenWidgets.Count > 0;
        bool orderChanged = !widgetOrder.SequenceEqual(DefaultWidgetOrder);

        if (!visibilityChanged && !orderChanged)
        {
            return;
        }

        hiddenWidgets.Clear();
        widgetOrder.Clear();
        widgetOrder.AddRange(DefaultWidgetOrder);

        Preferences.Default.Remove(HiddenWidgetsPreferenceKey);
        Preferences.Default.Remove(WidgetOrderPreferenceKey);
        PreferencesChanged?.Invoke();
    }

    private void MoveWidget(
        HomeWidgetId widgetId,
        bool searchEarlier)
    {
        int currentIndex = widgetOrder.IndexOf(widgetId);
        int siblingIndex = FindSiblingIndex(widgetId, searchEarlier);

        if (currentIndex < 0 || siblingIndex < 0)
        {
            return;
        }

        (widgetOrder[currentIndex], widgetOrder[siblingIndex]) =
            (widgetOrder[siblingIndex], widgetOrder[currentIndex]);

        SaveWidgetOrder();
        PreferencesChanged?.Invoke();
    }

    private int FindSiblingIndex(
        HomeWidgetId widgetId,
        bool searchEarlier)
    {
        int currentIndex = widgetOrder.IndexOf(widgetId);

        if (currentIndex < 0)
        {
            return -1;
        }

        int direction = searchEarlier ? -1 : 1;

        for (int index = currentIndex + direction;
             index >= 0 && index < widgetOrder.Count;
             index += direction)
        {
            if (IsSameLayoutSection(widgetId, widgetOrder[index]))
            {
                return index;
            }
        }

        return -1;
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

    private static HashSet<HomeWidgetId> LoadHiddenWidgets()
    {
        string value = Preferences.Default.Get(
            HiddenWidgetsPreferenceKey,
            string.Empty);

        HashSet<HomeWidgetId> widgets = new();

        foreach (string entry in SplitPreferenceValue(value))
        {
            if (Enum.TryParse(
                entry,
                ignoreCase: true,
                out HomeWidgetId widgetId) &&
                Enum.IsDefined(typeof(HomeWidgetId), widgetId))
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
            if (Enum.TryParse(
                entry,
                ignoreCase: true,
                out HomeWidgetId widgetId) &&
                Enum.IsDefined(typeof(HomeWidgetId), widgetId) &&
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

    private static IEnumerable<string> SplitPreferenceValue(string value)
    {
        return value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }

    private static bool IsSameLayoutSection(
        HomeWidgetId first,
        HomeWidgetId second)
    {
        return IsTopWidget(first) == IsTopWidget(second);
    }

    private static bool IsTopWidget(HomeWidgetId widgetId)
    {
        return widgetId is
            HomeWidgetId.ProjectOverview or
            HomeWidgetId.QuickActions;
    }
}
