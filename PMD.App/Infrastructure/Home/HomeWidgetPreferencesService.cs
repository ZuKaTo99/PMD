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

    private readonly HashSet<HomeWidgetId> hiddenWidgets;

    public HomeWidgetPreferencesService()
    {
        hiddenWidgets = LoadHiddenWidgets();
    }

    public event Action? PreferencesChanged;

    public bool IsVisible(HomeWidgetId widgetId)
    {
        return !hiddenWidgets.Contains(widgetId);
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

    public void ResetToDefaults()
    {
        if (hiddenWidgets.Count == 0)
        {
            return;
        }

        hiddenWidgets.Clear();
        Preferences.Default.Remove(HiddenWidgetsPreferenceKey);
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

    private static HashSet<HomeWidgetId> LoadHiddenWidgets()
    {
        string value = Preferences.Default.Get(
            HiddenWidgetsPreferenceKey,
            string.Empty);

        HashSet<HomeWidgetId> widgets = new();

        foreach (string entry in value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse(
                entry,
                ignoreCase: true,
                out HomeWidgetId widgetId))
            {
                widgets.Add(widgetId);
            }
        }

        return widgets;
    }
}
