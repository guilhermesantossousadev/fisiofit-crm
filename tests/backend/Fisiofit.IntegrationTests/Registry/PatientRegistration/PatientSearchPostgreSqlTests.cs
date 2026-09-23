using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Organization.Application;
using Fisiofit.Modules.Registry.Patients.Application;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.People.Application;
using Fisiofit.Modules.Registry.People.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.PatientRegistration;

[Collection(PatientRegistrationPostgreSqlCollection.Name)]
public sealed class PatientSearchPostgreSqlTests(PatientRegistrationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Search_NameIsCaseInsensitiveAccentSensitiveAndCpfPhoneAreExact()
    {
        var marker = Guid.CreateVersion7().ToString("N")[..8];
        var first = await SeedAsync($"Ágata Busca {marker}", "52998224725", new PhoneNumber("598", "2", "99112233"));
        _ = await SeedAsync($"Agata Busca {marker}", null, new PhoneNumber("598", "2", "99112234"));

        var accented = await SearchAsync($"ágata busca {marker}");
        var unaccented = await SearchAsync($"agata busca {marker}");
        var cpf = await SearchAsync("529.982.247-25");
        var phone = await SearchAsync("+598 2 99112233");
        var partialPhone = await SearchAsync("112233");

        Assert.True(accented.Success);
        Assert.Equal(first.PatientId, Assert.Single(accented.Value!.Items).PatientId);
        Assert.Single(unaccented.Value!.Items);
        Assert.DoesNotContain(unaccented.Value.Items, item => item.PatientId == first.PatientId);
        Assert.Equal(first.PatientId, Assert.Single(cpf.Value!.Items).PatientId);
        Assert.Equal("***.***.***-25", cpf.Value.Items[0].Cpf.Masked);
        Assert.Equal(first.PatientId, Assert.Single(phone.Value!.Items).PatientId);
        Assert.False(partialPhone.Success);
        Assert.Equal("VALIDATION_ERROR", partialPhone.Code);
    }

    [Fact]
    public async Task Search_PaginatesAfterFinalIntersectionWithStableAscendingAndDescendingOrder()
    {
        var marker = Guid.CreateVersion7().ToString("N")[..8];
        _ = await SeedAsync($"BuscaPagina {marker} Charlie", null, new PhoneNumber("597", "2", "88112231"));
        _ = await SeedAsync($"BuscaPagina {marker} Alpha", null, new PhoneNumber("597", "2", "88112232"));
        _ = await SeedAsync($"BuscaPagina {marker} Bravo", null, new PhoneNumber("597", "2", "88112233"));

        var firstPage = await SearchAsync($"BuscaPagina {marker}", page: "1", pageSize: "2");
        var secondPage = await SearchAsync($"BuscaPagina {marker}", page: "2", pageSize: "2");
        var descending = await SearchAsync($"BuscaPagina {marker}", sort: "-name");

        Assert.Equal(3, firstPage.Value!.TotalCount);
        Assert.Equal(2, firstPage.Value.Items.Count);
        Assert.Single(secondPage.Value!.Items);
        Assert.EndsWith("Alpha", firstPage.Value.Items[0].FullName, StringComparison.Ordinal);
        Assert.EndsWith("Charlie", descending.Value!.Items[0].FullName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_IntersectsUnitScopeAndStatusBeforePeopleAndDoesNotAddMigration()
    {
        var marker = Guid.CreateVersion7().ToString("N")[..8];
        var active = await SeedAsync($"EscopoBusca {marker} Ativo", null, new PhoneNumber("596", "2", "77112231"));
        var inactive = await SeedAsync(
            $"EscopoBusca {marker} Inativo",
            null,
            new PhoneNumber("596", "2", "77112232"),
            fixture.InactiveUnitId,
            inactive: true);

        var activeOnly = await SearchAsync(
            $"EscopoBusca {marker}",
            status: "ACTIVE",
            actorUnits: new HashSet<Guid> { fixture.ActiveUnitId, fixture.InactiveUnitId });
        var inactiveOnly = await SearchAsync(
            $"EscopoBusca {marker}",
            status: "INACTIVE",
            unit: fixture.InactiveUnitId.ToString("D"),
            actorUnits: new HashSet<Guid> { fixture.ActiveUnitId, fixture.InactiveUnitId });
        var oneUnitScope = await SearchAsync(
            $"EscopoBusca {marker}",
            actorUnits: new HashSet<Guid> { fixture.ActiveUnitId });

        Assert.Equal(active.PatientId, Assert.Single(activeOnly.Value!.Items).PatientId);
        Assert.Equal(inactive.PatientId, Assert.Single(inactiveOnly.Value!.Items).PatientId);
        Assert.Equal("INACTIVE", inactiveOnly.Value.Items[0].AdministrativeStatus);
        Assert.Equal(active.PatientId, Assert.Single(oneUnitScope.Value!.Items).PatientId);

        await using var patients = fixture.CreatePatientsDbContext();
        await using var people = fixture.CreatePeopleDbContext();
        Assert.Single(await patients.Database.GetAppliedMigrationsAsync());
        Assert.Single(await people.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task SelectiveCpfAndUnitPlans_CanUseApprovedExistingIndexes()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var disableSequentialScan = new NpgsqlCommand("SET enable_seqscan = off;", connection))
        {
            await disableSequentialScan.ExecuteNonQueryAsync();
        }

        var cpfPlan = await ExplainAsync(
            connection,
            "EXPLAIN (COSTS OFF) SELECT id FROM people.person WHERE cpf_normalized = @value;",
            "52998224725");
        var unitPlan = await ExplainAsync(
            connection,
            "EXPLAIN (COSTS OFF) SELECT person_id FROM patients.patient_profile WHERE primary_unit_id = @value::uuid;",
            fixture.ActiveUnitId.ToString("D"));

        Assert.Contains("ux_person__cpf_normalized", cpfPlan, StringComparison.Ordinal);
        Assert.Contains("ix_patient_profile__primary_unit_id", unitPlan, StringComparison.Ordinal);
    }

    private async Task<(Guid PatientId, Guid PersonId)> SeedAsync(
        string name,
        string? cpf,
        PhoneNumber phone,
        Guid? unitId = null,
        bool inactive = false)
    {
        await using var people = fixture.CreatePeopleDbContext();
        var person = Person.Create(name, new DateOnly(1990, 1, 1), cpf, phone);
        people.People.Add(person);
        await people.SaveChangesAsync();

        await using var patients = fixture.CreatePatientsDbContext();
        var profile = PatientProfile.Create(
            person.Id,
            unitId ?? fixture.ActiveUnitId,
            new DateOnly(2026, 9, 18));
        patients.PatientProfiles.Add(profile);
        await patients.SaveChangesAsync();
        if (inactive)
        {
            await patients.PatientProfiles
                .Where(candidate => candidate.Id == profile.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    candidate => candidate.AdministrativeStatus,
                    PatientAdministrativeStatus.Inactive));
        }

        return (profile.Id, person.Id);
    }

    private async Task<PatientApplicationResult<PatientListResponse>> SearchAsync(
        string? search,
        string? page = null,
        string? pageSize = null,
        string? sort = null,
        string? status = null,
        string? unit = null,
        IReadOnlySet<Guid>? actorUnits = null)
    {
        await using var organization = fixture.CreateOrganizationDbContext();
        await using var people = fixture.CreatePeopleDbContext();
        await using var patients = fixture.CreatePatientsDbContext();
        var handler = new SearchPatients(
            patients,
            new SearchPeopleForPatientList(people),
            new GetUnitForPatientRead(organization));
        var names = new List<string>();
        if (search is not null) names.Add("search");
        if (page is not null) names.Add("page");
        if (pageSize is not null) names.Add("pageSize");
        if (sort is not null) names.Add("sort");
        if (status is not null) names.Add("administrativeStatus");
        if (unit is not null) names.Add("primaryUnitId");

        return await handler.ExecuteAsync(new SearchPatientsQuery(
            search,
            unit,
            status,
            page,
            pageSize,
            sort,
            names,
            Actor(actorUnits ?? new HashSet<Guid> { fixture.ActiveUnitId, fixture.InactiveUnitId }),
            "integration-search-correlation"));
    }

    private static PatientRequestActor Actor(IReadOnlySet<Guid> units) => new(
        Guid.Parse("0199ffee-0000-7000-8000-000000000202"),
        true,
        true,
        false,
        new HashSet<string>(StringComparer.Ordinal)
        {
            PatientPermissions.ReadProfile,
            PeoplePermissions.ReadPerson
        },
        units);

    private static async Task<string> ExplainAsync(NpgsqlConnection connection, string sql, string value)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("value", value);
        await using var reader = await command.ExecuteReaderAsync();
        var lines = new List<string>();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
