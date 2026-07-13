using Microsoft.Maui.Storage;
using PMD.App.Application.Appearance;
using PMD.App.Domain.Appearance;

namespace PMD.App.Infrastructure.Appearance;

public sealed class AppThemeService : IAppThemeService
{
    private const string ThemePreferenceKey = "pmd-app-theme";

    public AppThemeService()
    {
        CurrentTheme = LoadTheme();
    }

    public event Action? ThemeChanged;

    public PmdTheme CurrentTheme { get; private set; }

    public bool IsDarkMode =>
        CurrentTheme == PmdTheme.Dark;

    public void SetTheme(PmdTheme theme)
    {
        if (CurrentTheme == theme)
        {
            return;
        }

        CurrentTheme = theme;

        Preferences.Default.Set(
            ThemePreferenceKey,
            theme.ToString());

        ThemeChanged?.Invoke();
    }

    public void ToggleTheme()
    {
        SetTheme(
            IsDarkMode
                ? PmdTheme.Light
                : PmdTheme.Dark);
    }

    private static PmdTheme LoadTheme()
    {
        string savedTheme = Preferences.Default.Get(
            ThemePreferenceKey,
            PmdTheme.Light.ToString());

        return Enum.TryParse(
            savedTheme,
            ignoreCase: true,
            out PmdTheme theme)
                ? theme
                : PmdTheme.Light;
    }
}
