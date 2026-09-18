using Fisiofit.Modules.Registry.Organization.Domain;
using Fisiofit.Modules.Registry.Organization.Infrastructure;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.PatientRegistration;

public sealed class PatientRegistrationPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fisiofit_patient_registration_tests")
        .WithUsername("fisiofit_test")
        .WithPassword("test-only-password")
        .Build();

    public Guid ClinicId { get; } = Guid.CreateVersion7();
    public Guid ActiveUnitId { get; } = Guid.CreateVersion7();
    public Guid InactiveUnitId { get; } = Guid.CreateVersion7();
    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await using (var organization = CreateOrganizationDbContext())
        {
            await organization.Database.MigrateAsync();
            var clinic = Clinic.Create(ClinicId, "Clínica Fictícia de Teste", "America/Sao_Paulo");
            var active = Unit.Create(ActiveUnitId, ClinicId, "Unidade Fictícia Ativa");
            var inactive = Unit.Create(InactiveUnitId, ClinicId, "Unidade Fictícia Inativa");
            inactive.Deactivate();
            organization.AddRange(clinic, active, inactive);
            await organization.SaveChangesAsync();
        }

        await using (var people = CreatePeopleDbContext())
        {
            await people.Database.MigrateAsync();
        }

        await using var patients = CreatePatientsDbContext();
        await patients.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await container.DisposeAsync();

    internal OrganizationDbContext CreateOrganizationDbContext() => new(
        new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    OrganizationDbContext.MigrationsHistoryTable,
                    OrganizationDbContext.Schema))
            .Options);

    internal PeopleDbContext CreatePeopleDbContext() => new(
        new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    PeopleDbContext.MigrationsHistoryTable,
                    PeopleDbContext.Schema))
            .Options);

    internal PatientsDbContext CreatePatientsDbContext() => new(
        new DbContextOptionsBuilder<PatientsDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    PatientsDbContext.MigrationsHistoryTable,
                    PatientsDbContext.Schema))
            .Options);
}

[CollectionDefinition(Name)]
public sealed class PatientRegistrationPostgreSqlCollection
    : ICollectionFixture<PatientRegistrationPostgreSqlFixture>
{
    public const string Name = "Patient registration PostgreSQL";
}
