using PMD.App.Application.Database;
using SQLite;
using SQLitePCL;

namespace PMD.App.Infrastructure.Database;

public sealed class PmdDatabaseConnectionFactory : IPmdDatabaseConnectionFactory
{
    private readonly IPmdDatabasePathProvider databasePathProvider;

    public PmdDatabaseConnectionFactory(IPmdDatabasePathProvider databasePathProvider)
    {
        this.databasePathProvider = databasePathProvider;

        Batteries_V2.Init();
    }

    public SQLiteConnection CreateConnection()
    {
        string databasePath = databasePathProvider.GetDatabasePath();

        return new SQLiteConnection(databasePath);
    }
}