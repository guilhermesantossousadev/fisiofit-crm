using Fisiofit.Modules.Registry.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.Patients.Infrastructure;

internal sealed class PatientsDbContext(DbContextOptions<PatientsDbContext> options) : DbContext(options)
{
    internal const string Schema = "patients";
    internal const string MigrationsHistoryTable = "__patients_migrations_history";

    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<PatientCommandReceipt> CommandReceipts => Set<PatientCommandReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PatientsDbContext).Assembly,
            type => type.Namespace?.StartsWith(
                "Fisiofit.Modules.Registry.Patients.Infrastructure.Configurations",
                StringComparison.Ordinal) == true);
    }
}
