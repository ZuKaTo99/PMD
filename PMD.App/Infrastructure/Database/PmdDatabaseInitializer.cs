using PMD.App.Application.Database;
using PMD.App.Infrastructure.Database.Entities;
using SQLite;
using SQLitePCL;

namespace PMD.App.Infrastructure.Database;

public sealed class PmdDatabaseInitializer : IPmdDatabaseInitializer
{
    private readonly IPmdDatabasePathProvider databasePathProvider;

    public PmdDatabaseInitializer(IPmdDatabasePathProvider databasePathProvider)
    {
        this.databasePathProvider = databasePathProvider;
    }

    public void Initialize()
    {
        Batteries_V2.Init();

        string databasePath = databasePathProvider.GetDatabasePath();

        using var connection = new SQLiteConnection(databasePath);

        connection.CreateTable<ProjectRecord>();
        connection.CreateTable<ProjectStateRecord>();
        connection.CreateTable<ProjectStateFileRecord>();
    }
}