using SQLite;

namespace PMD.App.Infrastructure.Database;

public interface IPmdDatabaseConnectionFactory
{
    SQLiteConnection CreateConnection();
}