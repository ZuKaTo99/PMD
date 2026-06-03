using Microsoft.Extensions.Logging;
using PMD.App.Application.Scanner;
using PMD.App.Infrastructure.Scanner;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;

namespace PMD.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<IProjectFolderScanner, ProjectFolderScanner>();
        builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}