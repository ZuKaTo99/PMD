using System;

namespace PMD.App.Application.Dashboard;

public interface IDashboardOverviewService
{
    event Action? OverviewChanged;

    DashboardOverview GetOverview();
}
