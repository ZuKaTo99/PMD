using PMD.App.Application.Database;
using PMD.App.Application.ProjectVersioning;

namespace PMD.App.Infrastructure.ProjectVersioning;

public sealed class PmdProjectVersionStorePathProvider
    : IProjectVersionStorePathProvider
{
    private const string VersionStoreFolderName =
        "ProjectVersionStore";

    private readonly IPmdDatabasePathProvider databasePathProvider;

    public PmdProjectVersionStorePathProvider(
        IPmdDatabasePathProvider databasePathProvider)
    {
        ArgumentNullException.ThrowIfNull(
            databasePathProvider);

        this.databasePathProvider =
            databasePathProvider;
    }

    public string GetProjectVersionStorePath(
        Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException(
                "Die Projekt-ID darf nicht leer sein.",
                nameof(projectId));
        }

        string databasePath = Path.GetFullPath(
            databasePathProvider.GetDatabasePath());

        string? appDataDirectory =
            Path.GetDirectoryName(databasePath);

        if (string.IsNullOrWhiteSpace(
                appDataDirectory))
        {
            throw new InvalidOperationException(
                "Der PMD-Anwendungsdatenordner konnte nicht bestimmt werden.");
        }

        return Path.Combine(
            appDataDirectory,
            VersionStoreFolderName,
            $"{projectId:N}.git");
    }
}