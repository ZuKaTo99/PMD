using PMD.App.Application.Database;

namespace PMD.App.Infrastructure.Database;

public sealed class PmdDatabasePathProvider : IPmdDatabasePathProvider
{
    private const string DatabaseFileName = "pmd.db3";

    public string GetDatabasePath()
    {
        string appDataFolder = FileSystem.AppDataDirectory;

        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        return Path.Combine(appDataFolder, DatabaseFileName);
    }
}