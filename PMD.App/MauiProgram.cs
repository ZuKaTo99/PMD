using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PMD.App.Application.Database;
using PMD.App.Infrastructure.Database;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Application.Scanner;
using PMD.App.Infrastructure.ProjectStates;
using PMD.App.Infrastructure.Projects;
using PMD.App.Infrastructure.Scanner;

namespace PMD.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<IProjectMemoryStore, ProjectMemoryStore>();
        builder.Services.AddSingleton<IProjectStateMemoryStore, ProjectStateMemoryStore>();
        builder.Services.AddSingleton<IProjectOverviewService, ProjectOverviewService>();
        builder.Services.AddSingleton<IProjectFolderScanner, ProjectFolderScanner>();
        builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
        builder.Services.AddSingleton<IPmdDatabasePathProvider, PmdDatabasePathProvider>();
        builder.Services.AddSingleton<IPmdDatabaseInitializer, PmdDatabaseInitializer>();
        builder.Services.AddSingleton<IPmdDatabaseConnectionFactory, PmdDatabaseConnectionFactory>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        MauiApp app = builder.Build();

        app.Services
            .GetRequiredService<IPmdDatabaseInitializer>()
            .Initialize();

        return app;
    }
}
