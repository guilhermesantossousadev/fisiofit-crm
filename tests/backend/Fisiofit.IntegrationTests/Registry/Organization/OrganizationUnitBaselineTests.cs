using Fisiofit.ModuleContracts.Organization;
using Fisiofit.Modules.Registry.Organization.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.Organization;

[Collection(PostgreSqlCollection.Name)]
public sealed class OrganizationUnitBaselineTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Migration_CreatesOnlyOrganizationBaselineAndItsHistory()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();

        while (await reader.ReadAsync())
        {
            tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        Assert.Equal(
            [
                "organization.__organization_migrations_history",
                "organization.clinic",
                "organization.unit"
            ],
            tables);
        Assert.DoesNotContain(tables, table => table.StartsWith("people.", StringComparison.Ordinal));
        Assert.DoesNotContain(tables, table => table.StartsWith("patients.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ClinicAndUnits_PersistWithRequiredRelationshipAndLifecycle()
    {
        await using var dbContext = fixture.CreateDbContext();

        var clinic = await dbContext.Clinics.SingleAsync(candidate => candidate.Id == fixture.ClinicId);
        var units = await dbContext.Units
            .Where(candidate => candidate.ClinicId == fixture.ClinicId)
            .OrderBy(candidate => candidate.Name)
            .ToListAsync();

        Assert.Equal("Clínica de Integração", clinic.Name);
        Assert.Equal(2, units.Count);
        Assert.All(units, unit => Assert.Equal(fixture.ClinicId, unit.ClinicId));
    }

    [Fact]
    public async Task UnitForeignKey_RejectsMissingClinic()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO organization.unit (id, clinic_id, name, status)
            VALUES (@id, @clinic_id, 'Unidade Órfã de Teste', 'ACTIVE');
            """,
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("clinic_id", Guid.CreateVersion7());

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }

    [Fact]
    public async Task ClinicDelete_IsRestrictedWhileUnitsExist()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "DELETE FROM organization.clinic WHERE id = @id;",
            connection);
        command.Parameters.AddWithValue("id", fixture.ClinicId);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.RestrictViolation, exception.SqlState);
    }

    [Fact]
    public async Task ValidationContract_ReturnsValidForActiveUnit()
    {
        await using var dbContext = fixture.CreateDbContext();
        IValidateUnitForPatientRegistration contract = new ValidateUnitForPatientRegistration(dbContext);

        var result = await contract.ValidateAsync(fixture.ActiveUnitId);

        Assert.Equal(UnitValidationOutcome.Valid, result.Outcome);
        Assert.Equal(fixture.ClinicId, result.ClinicId);
        Assert.Equal("Unidade Ativa de Integração", result.Name);
    }

    [Fact]
    public async Task ValidationContract_ReturnsInactiveForInactiveUnit()
    {
        await using var dbContext = fixture.CreateDbContext();
        IValidateUnitForPatientRegistration contract = new ValidateUnitForPatientRegistration(dbContext);

        var result = await contract.ValidateAsync(fixture.InactiveUnitId);

        Assert.Equal(UnitValidationOutcome.Inactive, result.Outcome);
        Assert.Null(result.ClinicId);
        Assert.Null(result.Name);
    }

    [Fact]
    public async Task ValidationContract_ReturnsNotFoundForUnknownUnit()
    {
        await using var dbContext = fixture.CreateDbContext();
        IValidateUnitForPatientRegistration contract = new ValidateUnitForPatientRegistration(dbContext);

        var result = await contract.ValidateAsync(Guid.CreateVersion7());

        Assert.Equal(UnitValidationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public void OrganizationDbContext_MapsOnlyOrganizationSchema()
    {
        using var dbContext = fixture.CreateDbContext();

        var schemas = dbContext.Model.GetEntityTypes()
            .Select(entityType => entityType.GetSchema())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Single(schemas);
        Assert.Equal("organization", schemas[0]);
        Assert.Equal(2, dbContext.Model.GetEntityTypes().Count());
    }
}
