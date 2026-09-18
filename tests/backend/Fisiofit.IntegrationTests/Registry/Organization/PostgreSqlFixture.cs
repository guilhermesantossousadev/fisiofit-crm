using Fisiofit.Modules.Registry.Organization.Domain;
using Fisiofit.Modules.Registry.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.Organization;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fisiofit_organization_tests")
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

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();

        var clinic = Clinic.Create(ClinicId, "Clínica de Integração", "America/Sao_Paulo");
        var activeUnit = Unit.Create(ActiveUnitId, ClinicId, "Unidade Ativa de Integração");
        var inactiveUnit = Unit.Create(InactiveUnitId, ClinicId, "Unidade Inativa de Integração");
        inactiveUnit.Deactivate();

        dbContext.Clinics.Add(clinic);
        dbContext.Units.AddRange(activeUnit, inactiveUnit);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await container.DisposeAsync();

    internal OrganizationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    OrganizationDbContext.MigrationsHistoryTable,
                    OrganizationDbContext.Schema))
            .Options;

        return new OrganizationDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "Organization PostgreSQL";
}
