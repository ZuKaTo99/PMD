using PMD.App.Application.Database;
using PMD.App.Infrastructure.Database.Entities;

namespace PMD.App.Infrastructure.Database;

public sealed class PmdDatabaseInitializer : IPmdDatabaseInitializer
{
    private readonly IPmdDatabaseConnectionFactory connectionFactory;

    public PmdDatabaseInitializer(IPmdDatabaseConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection = connectionFactory.CreateConnection();

        connection.CreateTable<ProjectRecord>();
        connection.CreateTable<ProjectStateRecord>();
        connection.CreateTable<ProjectStateFileRecord>();
    }
}