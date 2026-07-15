using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PMD.App.Application.Home;
using PMD.App.Domain.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMD.App.Features.Home.Pages;

public partial class HomePage : IDisposable, IAsyncDisposable
{
    private ElementReference homeWidgetGridElement;
    private DotNetObjectReference<HomePage>? dragDropReference;
    private bool isDisposed;

    [Inject]
    private IHomeOverviewService HomeOverviewService { get; set; } = default!;

    [Inject]
    private IHomeWidgetPreferencesService HomeWidgetPreferencesService
    {
        get;
        set;
    } = default!;

    [Inject]
    private IJSRuntime JavaScriptRuntime { get; set; } = default!;

    protected HomeOverview Overview { get; private set; } = new();

    protected bool AreWidgetSettingsOpen { get; private set; }

    protected string WidgetSettingsAriaExpanded =>
        AreWidgetSettingsOpen ? "true" : "false";

    protected IReadOnlyList<HomeWidgetId> VisibleWidgets =>
        HomeWidgetPreferencesService
            .GetWidgetOrder()
            .Where(IsWidgetVisible)
            .ToList();

    protected bool HasVisibleWidgets =>
        VisibleWidgets.Count > 0;

    protected override void OnInitialized()
    {
        HomeOverviewService.OverviewChanged += OnOverviewChanged;
        HomeWidgetPreferencesService.PreferencesChanged +=
            OnWidgetPreferencesChanged;

        RefreshOverview();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!AreWidgetSettingsOpen || !HasVisibleWidgets || isDisposed)
        {
            return;
        }

        dragDropReference ??= DotNetObjectReference.Create(this);

        await JavaScriptRuntime.InvokeVoidAsync(
            "pmdHomeWidgetDragDrop.initialize",
            homeWidgetGridElement,
            dragDropReference,
            new
            {
                itemSelector = ".home-widget-layout",
                handleSelector = ".home-widget-direct-drag-handle",
                draggingClass = "home-widget-is-dragging",
                dropTargetClass = "home-widget-is-drop-target"
            });
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        HomeOverviewService.OverviewChanged -= OnOverviewChanged;
        HomeWidgetPreferencesService.PreferencesChanged -=
            OnWidgetPreferencesChanged;
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();

        try
        {
            await JavaScriptRuntime.InvokeVoidAsync(
                "pmdHomeWidgetDragDrop.dispose",
                homeWidgetGridElement);
        }
        catch (JSException)
        {
            // The WebView can already be unavailable while the page is being disposed.
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during every disposal phase.
        }

        dragDropReference?.Dispose();
        dragDropReference = null;
    }

    [JSInvokable]
    public Task MoveWidgetFromJavaScript(
        string sourceWidgetValue,
        string targetWidgetValue)
    {
        if (TryParseWidgetId(sourceWidgetValue, out HomeWidgetId sourceWidgetId) &&
            TryParseWidgetId(targetWidgetValue, out HomeWidgetId targetWidgetId))
        {
            HomeWidgetPreferencesService.MoveTo(
                sourceWidgetId,
                targetWidgetId);
        }

        return Task.CompletedTask;
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

    protected string GetWidgetSizeLabel(HomeWidgetId widgetId)
    {
        return HomeWidgetPreferencesService.GetSize(widgetId) switch
        {
            HomeWidgetSize.Compact => "Mini-Karte",
            HomeWidgetSize.Standard => "Karteikarte",
            HomeWidgetSize.Wide => "Breite Karte",
            HomeWidgetSize.Full => "Volle Zeile",
            _ => "Karteikarte"
        };
    }

    protected static string GetWidgetTitle(HomeWidgetId widgetId)
    {
        return widgetId switch
        {
            HomeWidgetId.ProjectOverview => "Projektübersicht",
            HomeWidgetId.QuickActions => "Schnellzugriffe",
            HomeWidgetId.RecentProjects => "Zuletzt geprüfte Projekte",
            HomeWidgetId.RecentChecks => "Projektentwicklung",
            HomeWidgetId.LanguageUsage => "Sprachen",
            _ => "Widget"
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
}
