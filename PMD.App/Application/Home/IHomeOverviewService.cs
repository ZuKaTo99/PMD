using System;

namespace PMD.App.Application.Home;

public interface IHomeOverviewService
{
    event Action? OverviewChanged;

    HomeOverview GetOverview();
}
