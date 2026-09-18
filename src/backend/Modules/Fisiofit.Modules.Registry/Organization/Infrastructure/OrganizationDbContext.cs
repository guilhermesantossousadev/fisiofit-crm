using Fisiofit.Modules.Registry.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.Organization.Infrastructure;

internal sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : DbContext(options)
{
    internal const string Schema = "organization";
    internal const string MigrationsHistoryTable = "__organization_migrations_history";

    internal DbSet<Clinic> Clinics => Set<Clinic>();

    internal DbSet<Unit> Units => Set<Unit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OrganizationDbContext).Assembly,
            type => type.Namespace == $"{typeof(OrganizationDbContext).Namespace}.Configurations");
    }
}
