using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fisiofit.Modules.Registry.Patients.Infrastructure;

internal sealed class PatientsDbContextFactory : IDesignTimeDbContextFactory<PatientsDbContext>
{
    public PatientsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Database before using Patients migration tooling.");
        }
        var options = new DbContextOptionsBuilder<PatientsDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    PatientsDbContext.MigrationsHistoryTable,
                    PatientsDbContext.Schema))
            .Options;

        return new PatientsDbContext(options);
    }
}
