using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PMD.App.Application.Appearance;
using PMD.App.Application.Home;
using PMD.App.Application.Dashboard;
using PMD.App.Application.Database;
using PMD.App.Infrastructure.Appearance;
using PMD.App.Infrastructure.Home;
using PMD.App.Infrastructure.Database;
using PMD.App.Application.ProjectStates;
using PMD.App.Application.Projects;
using PMD.App.Application.Scanner;
using PMD.App.Infrastructure.ProjectStates;
using PMD.App.Infrastructure.Projects;
using PMD.App.Infrastructure.Scanner;
using PMD.App.Application.ProjectFiles;
using PMD.App.Application.ProjectHistory;
using PMD.App.Infrastructure.ProjectFiles;
using PMD.App.Application.ProjectChanges;
using PMD.App.Application.ProjectCodeDiff;
using PMD.App.Application.Kanban;
using PMD.App.Infrastructure.Kanban;

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

        builder.Services.AddSingleton<IAppThemeService, AppThemeService>();
        builder.Services.AddSingleton<IHomeOverviewService, HomeOverviewService>();
        builder.Services.AddSingleton<IDashboardOverviewService, DashboardOverviewService>();
        builder.Services.AddSingleton<IHomeWidgetPreferencesService, HomeWidgetPreferencesService>();
        builder.Services.AddSingleton<IProjectMemoryStore, ProjectMemoryStore>();
        builder.Services.AddSingleton<IProjectStateMemoryStore, ProjectStateMemoryStore>();
        builder.Services.AddSingleton<IProjectOverviewService, ProjectOverviewService>();
        builder.Services.AddSingleton<IProjectHistoryService, ProjectHistoryService>();
        builder.Services.AddSingleton<IProjectFolderLauncher, ProjectFolderLauncher>();
        builder.Services.AddSingleton<IProjectFolderScanner, ProjectFolderScanner>();
        builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
        builder.Services.AddSingleton<IPmdDatabasePathProvider, PmdDatabasePathProvider>();
        builder.Services.AddSingleton<IPmdDatabaseInitializer, PmdDatabaseInitializer>();
        builder.Services.AddSingleton<IPmdDatabaseConnectionFactory, PmdDatabaseConnectionFactory>();
        builder.Services.AddSingleton<IProjectRepository, SqliteProjectRepository>();
        builder.Services.AddSingleton<IProjectStateRepository, SqliteProjectStateRepository>();
        builder.Services.AddSingleton<IProjectFileContentReader, ProjectFileContentReader>();
        builder.Services.AddSingleton<IProjectChangesService, ProjectChangesService>();
        builder.Services.AddSingleton<IProjectCodeDiffService, ProjectCodeDiffService>();
        builder.Services.AddSingleton<IKanbanTaskRepository, SqliteKanbanTaskRepository>();
        builder.Services.AddSingleton<IKanbanBoardService, KanbanBoardService>();

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
