using PMD.App.Domain.Home;
using System;

namespace PMD.App.Application.Home;

public interface IHomeWidgetPreferencesService
{
    event Action? PreferencesChanged;

    bool IsVisible(HomeWidgetId widgetId);

    void SetVisibility(
        HomeWidgetId widgetId,
        bool isVisible);

    void ResetToDefaults();
}
