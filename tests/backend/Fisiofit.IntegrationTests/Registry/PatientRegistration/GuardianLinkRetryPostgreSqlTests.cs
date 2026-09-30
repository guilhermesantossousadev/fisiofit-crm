using System.Data.Common;
using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Application;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.PatientRegistration;

[Collection(PatientRegistrationPostgreSqlCollection.Name)]
public sealed class GuardianLinkRetryPostgreSqlTests(PatientRegistrationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task Create_RetriesAbortedPostgreSqlTransaction_ReacquiresLockAndPersistsOneLink()
    {
        var patient = await AddPatientAsync();
        var faults = new LockFaultInterceptor(PostgresErrorCodes.SerializationFailure, failures: 1);
        await using var db = fixture.CreatePatientsDbContext(faults);

        var result = await Handler(db).CreateAsync(CreateCommand(patient.Id));

        Assert.True(result.Success);
        Assert.Equal(2, faults.LockAttempts);
        await using var verify = fixture.CreatePatientsDbContext();
        var links = await verify.GuardianLinks.Where(x => x.PatientProfileId == patient.Id).ToListAsync();
        var link = Assert.Single(links);
        Assert.Equal(result.Value!.GuardianLinkId, link.Id);
        Assert.Equal(1, link.Version);
        await AssertNoIdleTransactionsAsync();
    }

    [Fact]
    public async Task End_RetriesAbortedPostgreSqlTransaction_ReloadsVersionAndEndsExactlyOnce()
    {
        var patient = await AddPatientAsync();
        var original = await AddCurrentLinkAsync(patient.Id);
        var faults = new LockFaultInterceptor(PostgresErrorCodes.DeadlockDetected, failures: 1);
        await using var db = fixture.CreatePatientsDbContext(faults);

        var result = await Handler(db).EndAsync(new(patient.Id, original.Id, Today().AddDays(1), Etag(original), Actor()));

        Assert.True(result.Success);
        Assert.Equal(2, faults.LockAttempts);
        await using var verify = fixture.CreatePatientsDbContext();
        var link = await verify.GuardianLinks.SingleAsync(x => x.Id == original.Id);
        Assert.Equal(Today().AddDays(1), link.EffectiveTo);
        Assert.Equal(2, link.Version);
        Assert.NotNull(link.EndedAt);
        Assert.Equal(1, await verify.GuardianLinks.CountAsync(x => x.Id == original.Id));
        await AssertNoIdleTransactionsAsync();
    }

    [Fact]
    public async Task Create_StopsAfterTwoAbortedTransactions_AndConfirmsNoMutation()
    {
        var patient = await AddPatientAsync();
        var faults = new LockFaultInterceptor(PostgresErrorCodes.SerializationFailure, failures: 2);
        await using var db = fixture.CreatePatientsDbContext(faults);

        var result = await Handler(db).CreateAsync(CreateCommand(patient.Id));

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("INTERNAL_ERROR", result.Code);
        Assert.Equal(2, faults.LockAttempts);
        await using var verify = fixture.CreatePatientsDbContext();
        Assert.Equal(0, await verify.GuardianLinks.CountAsync(x => x.PatientProfileId == patient.Id));
        await AssertNoIdleTransactionsAsync();
    }

    [Fact]
    public async Task Create_CommitTimeoutHasUnknownOutcome_AndDoesNotRetry()
    {
        var patient = await AddPatientAsync();
        var faults = new CommitTimeoutInterceptor();
        var locks = new LockFaultInterceptor(null, failures: 0);
        await using var db = fixture.CreatePatientsDbContext(locks, faults);

        var result = await Handler(db).CreateAsync(CreateCommand(patient.Id));

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("INTERNAL_ERROR", result.Code);
        Assert.Equal(1, faults.CommitAttempts);
        Assert.Equal(1, locks.LockAttempts);
    }

    [Fact]
    public async Task Create_OverlapAndPrimaryConflict_DoNotRetry()
    {
        var patient = await AddPatientAsync();
        var guardian = Guid.CreateVersion7();
        await AddCurrentLinkAsync(patient.Id, guardian, isPrimary: true);
        var faults = new LockFaultInterceptor(null, failures: 0);
        await using var db = fixture.CreatePatientsDbContext(faults);

        var overlap = await Handler(db).CreateAsync(CreateCommand(patient.Id, guardian, isPrimary: false));
        var primary = await Handler(db).CreateAsync(CreateCommand(patient.Id, Guid.CreateVersion7(), isPrimary: true));

        Assert.Equal("CONFLICT", overlap.Code);
        Assert.Equal("CONFLICT", primary.Code);
        Assert.Equal(2, faults.LockAttempts);
    }

    [Fact]
    public async Task End_StaleEtag_DoesNotRetry()
    {
        var patient = await AddPatientAsync();
        var link = await AddCurrentLinkAsync(patient.Id);
        var faults = new LockFaultInterceptor(null, failures: 0);
        await using var db = fixture.CreatePatientsDbContext(faults);

        var result = await Handler(db).EndAsync(new(patient.Id, link.Id, Today().AddDays(1), "\"guardian-stale-v1\"", Actor()));

        Assert.False(result.Success);
        Assert.Equal(412, result.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", result.Code);
        Assert.Equal(1, faults.LockAttempts);
    }

    [Fact]
    public async Task Create_NonTransientDatabaseConstraintError_DoesNotRetry()
    {
        var patient = await AddPatientAsync();
        var faults = new LockFaultInterceptor(PostgresErrorCodes.UniqueViolation, failures: 1);
        await using var db = fixture.CreatePatientsDbContext(faults);

        var result = await Handler(db).CreateAsync(CreateCommand(patient.Id));

        Assert.False(result.Success);
        Assert.Equal("INTERNAL_ERROR", result.Code);
        Assert.Equal(1, faults.LockAttempts);
        await using var verify = fixture.CreatePatientsDbContext();
        Assert.Equal(0, await verify.GuardianLinks.CountAsync(x => x.PatientProfileId == patient.Id));
    }

    private async Task<PatientProfile> AddPatientAsync()
    {
        var patient = PatientProfile.Create(Guid.CreateVersion7(), fixture.ActiveUnitId, Today());
        await using var db = fixture.CreatePatientsDbContext();
        db.PatientProfiles.Add(patient);
        await db.SaveChangesAsync();
        return patient;
    }

    private async Task<GuardianLink> AddCurrentLinkAsync(Guid patientId, Guid? guardianId = null, bool isPrimary = false)
    {
        var link = GuardianLink.Create(patientId, guardianId ?? Guid.CreateVersion7(), Today(), null, isPrimary, Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        await using var db = fixture.CreatePatientsDbContext();
        db.GuardianLinks.Add(link);
        await db.SaveChangesAsync();
        return link;
    }

    private ManageGuardianLinks Handler(PatientsDbContext db) => new(db, new FoundPeople(), NullLogger<ManageGuardianLinks>.Instance);
    private GuardianLinkCommand CreateCommand(Guid patientId, Guid? guardianId = null, bool isPrimary = false) => new(patientId, guardianId ?? Guid.CreateVersion7(), Today(), null, isPrimary, Actor());
    private PatientRequestActor Actor() => new(Guid.CreateVersion7(), true, true, false, new HashSet<string>(["patients.guardian.manage", PeoplePermissions.ReadPerson]), new HashSet<Guid>([fixture.ActiveUnitId]));
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    private static string Etag(GuardianLink link) => $"\"guardian-{link.Id:N}-v{link.Version}\"";

    private async Task AssertNoIdleTransactionsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'idle in transaction'", connection);
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }

    private sealed class FoundPeople : IGetPersonForGuardianLink
    {
        public Task<GuardianPersonResult> GetAsync(Guid personId, PeopleActorContext actor, CancellationToken cancellationToken = default) => Task.FromResult(new GuardianPersonResult(GuardianPersonOutcome.Found, new GuardianPersonData(personId, "Test Guardian", new DateOnly(1980, 1, 1))));
        public Task<IReadOnlyDictionary<Guid, GuardianPersonData>?> GetManyAsync(IReadOnlyCollection<Guid> personIds, PeopleActorContext actor, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<Guid, GuardianPersonData>?>(personIds.ToDictionary(x => x, x => new GuardianPersonData(x, "Test Guardian", new DateOnly(1980, 1, 1))));
    }

    private sealed class LockFaultInterceptor(string? sqlState, int failures) : DbCommandInterceptor
    {
        private int remaining = failures;
        public int LockAttempts { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)) return ValueTask.FromResult(result);
            LockAttempts++;
            if (remaining-- > 0)
            {
                command.Parameters.Clear();
                command.CommandText = $"DO $$ BEGIN RAISE EXCEPTION USING ERRCODE = '{sqlState}'; END $$;";
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CommitTimeoutInterceptor : DbTransactionInterceptor
    {
        public int CommitAttempts { get; private set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            CommitAttempts++;
            throw new TimeoutException("Test-only commit outcome fault.");
        }
    }
}
