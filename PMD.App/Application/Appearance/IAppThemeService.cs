using PMD.App.Domain.Appearance;

namespace PMD.App.Application.Appearance;

public interface IAppThemeService
{
    event Action? ThemeChanged;

    PmdTheme CurrentTheme { get; }

    bool IsDarkMode { get; }

    void SetTheme(PmdTheme theme);

    void ToggleTheme();
}
