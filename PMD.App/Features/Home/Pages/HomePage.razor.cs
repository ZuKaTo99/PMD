using Microsoft.AspNetCore.Components;
using PMD.App.Application.Home;
using System;

namespace PMD.App.Features.Home.Pages;

public partial class HomePage : IDisposable
{
    [Inject]
    private IHomeOverviewService HomeOverviewService { get; set; } = default!;

    protected HomeOverview Overview { get; private set; } = new();

    protected override void OnInitialized()
    {
        HomeOverviewService.OverviewChanged += OnOverviewChanged;
        RefreshOverview();
    }

    public void Dispose()
    {
        HomeOverviewService.OverviewChanged -= OnOverviewChanged;
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
        Overview = HomeOverviewService.GetOverview();
    }
}
