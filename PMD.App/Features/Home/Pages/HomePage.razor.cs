using Microsoft.AspNetCore.Components;
using PMD.App.Application.Home;
using PMD.App.Domain.Home;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMD.App.Features.Home.Pages;

public partial class HomePage : IDisposable
{
    [Inject]
    private IHomeOverviewService HomeOverviewService { get; set; } = default!;

    [Inject]
    private IHomeWidgetPreferencesService HomeWidgetPreferencesService
    {
        get;
        set;
    } = default!;

    protected HomeOverview Overview { get; private set; } = new();

    protected bool AreWidgetSettingsOpen { get; private set; }

    protected string WidgetSettingsAriaExpanded =>
        AreWidgetSettingsOpen ? "true" : "false";

    protected IReadOnlyList<HomeWidgetId> VisibleTopWidgets =>
        HomeWidgetPreferencesService
            .GetWidgetOrder()
            .Where(IsTopWidget)
            .Where(IsWidgetVisible)
            .ToList();

    protected IReadOnlyList<HomeWidgetId> VisibleRecentWidgets =>
        HomeWidgetPreferencesService
            .GetWidgetOrder()
            .Where(widgetId => !IsTopWidget(widgetId))
            .Where(IsWidgetVisible)
            .ToList();

    protected bool HasVisibleTopWidgets =>
        VisibleTopWidgets.Count > 0;

    protected bool HasVisibleRecentWidgets =>
        VisibleRecentWidgets.Count > 0;

    protected bool HasVisibleWidgets =>
        HasVisibleTopWidgets ||
        HasVisibleRecentWidgets;

    protected override void OnInitialized()
    {
        HomeOverviewService.OverviewChanged += OnOverviewChanged;
        HomeWidgetPreferencesService.PreferencesChanged +=
            OnWidgetPreferencesChanged;

        RefreshOverview();
    }

    public void Dispose()
    {
        HomeOverviewService.OverviewChanged -= OnOverviewChanged;
        HomeWidgetPreferencesService.PreferencesChanged -=
            OnWidgetPreferencesChanged;
    }

    protected bool IsWidgetVisible(HomeWidgetId widgetId)
    {
        return HomeWidgetPreferencesService.IsVisible(widgetId);
    }

    protected string GetWidgetSizeClass(HomeWidgetId widgetId)
    {
        return HomeWidgetPreferencesService.GetSize(widgetId) switch
        {
            HomeWidgetSize.Compact => "home-widget-size-compact",
            HomeWidgetSize.Standard => "home-widget-size-standard",
            HomeWidgetSize.Wide => "home-widget-size-wide",
            HomeWidgetSize.Full => "home-widget-size-full",
            _ => "home-widget-size-standard"
        };
    }

    protected void ToggleWidgetSettings()
    {
        AreWidgetSettingsOpen = !AreWidgetSettingsOpen;
    }

    protected void OpenWidgetSettings()
    {
        AreWidgetSettingsOpen = true;
    }

    private void OnOverviewChanged()
    {
        _ = InvokeAsync(() =>
        {
            RefreshOverview();
            StateHasChanged();
        });
    }

    private void OnWidgetPreferencesChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void RefreshOverview()
    {
        Overview = HomeOverviewService.GetOverview();
    }

    private static bool IsTopWidget(HomeWidgetId widgetId)
    {
        return widgetId is
            HomeWidgetId.ProjectOverview or
            HomeWidgetId.QuickActions;
    }
}
