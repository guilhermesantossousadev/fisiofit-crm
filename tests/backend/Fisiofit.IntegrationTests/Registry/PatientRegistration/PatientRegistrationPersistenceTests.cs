using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.People.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.PatientRegistration;

[Collection(PatientRegistrationPostgreSqlCollection.Name)]
public sealed class PatientRegistrationPersistenceTests(PatientRegistrationPostgreSqlFixture fixture)
{
    private static readonly PeopleActorContext PeopleActor = new(
        Guid.Parse("0199ffee-0000-7000-8000-000000000101"),
        true,
        true,
        false,
        [PeoplePermissions.CreatePerson, PeoplePermissions.ReadPerson]);

    [Fact]
    public async Task Migrations_CreateThreeIsolatedSchemasAndHistories()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_schema IN ('organization', 'people', 'patients')
            ORDER BY table_schema, table_name;
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        Assert.Contains("organization.__organization_migrations_history", tables);
        Assert.Contains("people.__people_migrations_history", tables);
        Assert.Contains("patients.__patients_migrations_history", tables);
        Assert.Contains("people.person", tables);
        Assert.Contains("people.contact_point", tables);
        Assert.Contains("people.command_receipt", tables);
        Assert.Contains("patients.patient_profile", tables);
        Assert.Contains("patients.command_receipt", tables);
    }

    [Fact]
    public async Task Patients_HasNoForeignKeyToPeopleOrOrganization()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints tc
            JOIN information_schema.constraint_column_usage ccu
              ON ccu.constraint_name = tc.constraint_name
             AND ccu.constraint_schema = tc.constraint_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND tc.table_schema = 'patients'
              AND ccu.table_schema IN ('people', 'organization');
            """,
            connection);

        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task People_PersistsPersonPhoneAndReceiptAndReplaysOperation()
    {
        await using var db = fixture.CreatePeopleDbContext();
        var handler = new CreatePersonForPatientRegistration(db);
        var operationKey = $"{Guid.CreateVersion7():D}/person-step/v1";
        var request = Request(operationKey, null);

        var created = await handler.ExecuteAsync(request);
        var replayed = await handler.ExecuteAsync(request);

        Assert.Equal(CreatePersonForPatientRegistrationOutcome.Created, created.Outcome);
        Assert.Equal(CreatePersonForPatientRegistrationOutcome.Replayed, replayed.Outcome);
        Assert.Equal(created.PersonId, replayed.PersonId);
        Assert.Equal(1, await db.People.CountAsync(person => person.Id == created.PersonId));
        Assert.Equal(1, await db.ContactPoints.CountAsync(phone => phone.PersonId == created.PersonId));
        Assert.Equal(1, await db.CommandReceipts.CountAsync(receipt => receipt.PersonId == created.PersonId));
    }

    [Fact]
    public async Task People_AllowsMultipleNullCpfAndRejectsDuplicatePresentCpf()
    {
        await using var db = fixture.CreatePeopleDbContext();
        var handler = new CreatePersonForPatientRegistration(db);

        var nullCpfOne = await handler.ExecuteAsync(Request($"{Guid.CreateVersion7():D}/person-step/v1", null));
        var nullCpfTwo = await handler.ExecuteAsync(Request($"{Guid.CreateVersion7():D}/person-step/v1", null));
        var cpfOne = await handler.ExecuteAsync(Request($"{Guid.CreateVersion7():D}/person-step/v1", "11144477735"));
        var cpfTwo = await handler.ExecuteAsync(Request($"{Guid.CreateVersion7():D}/person-step/v1", "111.444.777-35"));

        Assert.Equal(CreatePersonForPatientRegistrationOutcome.Created, nullCpfOne.Outcome);
        Assert.Equal(CreatePersonForPatientRegistrationOutcome.Created, nullCpfTwo.Outcome);
        Assert.Equal(CreatePersonForPatientRegistrationOutcome.Created, cpfOne.Outcome);
        Assert.Equal(CreatePersonForPatientRegistrationOutcome.CpfConflict, cpfTwo.Outcome);
        Assert.Equal(1, await db.People.CountAsync(person => person.CpfNormalized == "11144477735"));
    }

    [Fact]
    public async Task PatientProfile_DatabaseGuaranteesOneProfilePerPerson()
    {
        await using var db = fixture.CreatePatientsDbContext();
        var personId = Guid.CreateVersion7();
        db.PatientProfiles.Add(PatientProfile.Create(personId, fixture.ActiveUnitId, new DateOnly(2026, 9, 18)));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.PatientProfiles.Add(PatientProfile.Create(personId, fixture.ActiveUnitId, new DateOnly(2026, 9, 18)));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ux_patient_profile__person_id", postgres.ConstraintName);
    }

    [Fact]
    public void DbContexts_MapOnlyTheirOwnerSchema()
    {
        using var people = fixture.CreatePeopleDbContext();
        using var patients = fixture.CreatePatientsDbContext();

        Assert.All(people.Model.GetEntityTypes(), entity => Assert.Equal("people", entity.GetSchema()));
        Assert.All(patients.Model.GetEntityTypes(), entity => Assert.Equal("patients", entity.GetSchema()));
        Assert.Equal(3, people.Model.GetEntityTypes().Count());
        Assert.Equal(2, patients.Model.GetEntityTypes().Count());
    }

    private static CreatePersonForPatientRegistrationRequest Request(string operationKey, string? cpf) =>
        new(
            "Pessoa Fictícia de Integração",
            new DateOnly(1990, 1, 1),
            new PatientRegistrationPhone("999", "000", "000000"),
            cpf,
            operationKey,
            new string('a', 64),
            PeopleActor,
            "integration-test-correlation");
}
