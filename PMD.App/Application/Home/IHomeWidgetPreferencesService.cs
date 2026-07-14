using PMD.App.Domain.Home;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Home;

public interface IHomeWidgetPreferencesService
{
    event Action? PreferencesChanged;

    bool IsVisible(HomeWidgetId widgetId);

    HomeWidgetSize GetSize(HomeWidgetId widgetId);

    IReadOnlyList<HomeWidgetId> GetWidgetOrder();

    void SetVisibility(
        HomeWidgetId widgetId,
        bool isVisible);

    void SetSize(
        HomeWidgetId widgetId,
        HomeWidgetSize size);

    void MoveTo(
        HomeWidgetId widgetId,
        HomeWidgetId targetWidgetId);

    void ResetToDefaults();
}
