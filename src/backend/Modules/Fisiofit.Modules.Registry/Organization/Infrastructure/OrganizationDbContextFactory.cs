using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fisiofit.Modules.Registry.Organization.Infrastructure;

internal sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Database before using Organization migration tooling.");
        }

        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    OrganizationDbContext.MigrationsHistoryTable,
                    OrganizationDbContext.Schema))
            .Options;

        return new OrganizationDbContext(options);
    }
}
