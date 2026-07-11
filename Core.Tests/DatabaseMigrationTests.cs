using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Core.Tests;

public class DatabaseMigrationTests
{
    [Fact]
    public void Migrate_CreatesDatabaseWithInitialSchema()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"arciquiz-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<ArciQuizDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            using (var database = new ArciQuizDbContext(options))
            {
                database.Database.Migrate();

                Assert.True(database.Database.CanConnect());
                Assert.Contains(
                    database.Database.GetAppliedMigrations(),
                    migration => migration.EndsWith("InitialCreate", StringComparison.Ordinal));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }
}
