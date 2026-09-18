using Fisiofit.Modules.Registry.People.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.People.Infrastructure;

internal sealed class PeopleDbContext(DbContextOptions<PeopleDbContext> options) : DbContext(options)
{
    internal const string Schema = "people";
    internal const string MigrationsHistoryTable = "__people_migrations_history";

    public DbSet<Person> People => Set<Person>();
    public DbSet<ContactPoint> ContactPoints => Set<ContactPoint>();
    public DbSet<PeopleCommandReceipt> CommandReceipts => Set<PeopleCommandReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PeopleDbContext).Assembly,
            type => type.Namespace?.StartsWith(
                "Fisiofit.Modules.Registry.People.Infrastructure.Configurations",
                StringComparison.Ordinal) == true);
    }
}
