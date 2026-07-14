using PMD.App.Domain.Home;
using System;
using System.Collections.Generic;

namespace PMD.App.Application.Home;

public interface IHomeWidgetPreferencesService
{
    event Action? PreferencesChanged;

    bool IsVisible(HomeWidgetId widgetId);

    IReadOnlyList<HomeWidgetId> GetWidgetOrder();

    bool CanMoveEarlier(HomeWidgetId widgetId);

    bool CanMoveLater(HomeWidgetId widgetId);

    void SetVisibility(
        HomeWidgetId widgetId,
        bool isVisible);

    void MoveEarlier(HomeWidgetId widgetId);

    void MoveLater(HomeWidgetId widgetId);

    void ResetToDefaults();
}
