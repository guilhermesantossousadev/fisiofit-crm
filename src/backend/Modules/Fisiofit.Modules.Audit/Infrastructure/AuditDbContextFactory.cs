using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fisiofit.Modules.Audit.Infrastructure;

internal sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FISIOFIT_AUDIT_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=fisiofit;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable(AuditDbContext.MigrationsHistoryTable, AuditDbContext.Schema)).Options;
        return new AuditDbContext(options);
    }
}
