using Fisiofit.Modules.Registry.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.PatientRegistration;

[Collection(PatientRegistrationPostgreSqlCollection.Name)]
public sealed class GuardianLinkPersistenceTests(PatientRegistrationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Migration_CreatesPatientsOwnedGuardianLinkWithLocalForeignKey()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'patients' AND table_name = 'guardian_link';
            """, connection);
        Assert.Equal(1L, await command.ExecuteScalarAsync());

        command.CommandText = """SELECT COUNT(*) FROM pg_constraint WHERE conname = 'fk_guardian_link__patient_profile' AND pg_get_constraintdef(oid) LIKE '%patients.patient_profile%'""";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task GuardianLink_PersistsHistoryAndVersionWithoutCrossContextForeignKey()
    {
        var personId = Guid.CreateVersion7();
        var profile = PatientProfile.Create(personId, fixture.ActiveUnitId, new DateOnly(2026, 9, 24));
        await using (var db = fixture.CreatePatientsDbContext())
        {
            db.PatientProfiles.Add(profile);
            var link = GuardianLink.Create(profile.Id, Guid.CreateVersion7(), new DateOnly(2026, 9, 24), null, true, Guid.CreateVersion7(), DateTimeOffset.UtcNow);
            link.End(new DateOnly(2026, 9, 25), Guid.CreateVersion7(), DateTimeOffset.UtcNow);
            db.GuardianLinks.Add(link);
            await db.SaveChangesAsync();
        }
        await using (var verify = fixture.CreatePatientsDbContext())
        {
            var link = await verify.GuardianLinks.SingleAsync(x => x.PatientProfileId == profile.Id);
            Assert.Equal(2, link.Version);
            Assert.NotNull(link.EffectiveTo);
            Assert.NotNull(link.EndedAt);
        }
    }
}
