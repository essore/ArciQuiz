using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrasctructure.Data;

public class ArciQuizDbContextFactory : IDesignTimeDbContextFactory<ArciQuizDbContext>
{
    public ArciQuizDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ArciQuizDbContext>();
        var databasePath = Path.Combine(Path.GetTempPath(), "arciquiz-design-time.db");

        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new ArciQuizDbContext(optionsBuilder.Options);
    }
}
