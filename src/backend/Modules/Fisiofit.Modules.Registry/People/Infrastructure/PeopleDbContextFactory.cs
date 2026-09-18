using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fisiofit.Modules.Registry.People.Infrastructure;

internal sealed class PeopleDbContextFactory : IDesignTimeDbContextFactory<PeopleDbContext>
{
    public PeopleDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Database before using People migration tooling.");
        }
        var options = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    PeopleDbContext.MigrationsHistoryTable,
                    PeopleDbContext.Schema))
            .Options;

        return new PeopleDbContext(options);
    }
}
