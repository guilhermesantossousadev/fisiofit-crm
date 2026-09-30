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
public sealed class GuardianLinkConcurrencyPostgreSqlTests(PatientRegistrationPostgreSqlFixture fixture)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task CreateCreate_SameGuardian_UsesPatientLockAndPersistsExactlyOneLink()
    {
        var patient = await AddPatientAsync();
        var guardian = Guid.CreateVersion7();
        var from = Today().AddDays(1);
        var first = CreateCommand(patient.Id, guardian, from, null, false);
        var second = CreateCommand(patient.Id, guardian, from.AddDays(1), null, false);

        var results = await RunContendedAsync(
            db => Handler(db).CreateAsync(first),
            db => Handler(db).CreateAsync(second));

        Assert.True(results.First.Success);
        Assert.False(results.Second.Success);
        Assert.Equal(409, results.Second.StatusCode);
        Assert.Equal("CONFLICT", results.Second.Code);
        await using var verify = fixture.CreatePatientsDbContext();
        var links = await verify.GuardianLinks.Where(x => x.PatientProfileId == patient.Id).ToListAsync();
        var link = Assert.Single(links);
        Assert.Equal(guardian, link.GuardianPersonId);
        Assert.Equal(from, link.EffectiveFrom);
    }

    [Fact]
    public async Task CreateCreate_OverlappingPrimaryGuardians_ConfirmsOnlyOnePrimary()
    {
        var patient = await AddPatientAsync();
        var from = Today().AddDays(1);

        var results = await RunContendedAsync(
            db => Handler(db).CreateAsync(CreateCommand(patient.Id, Guid.CreateVersion7(), from, null, true)),
            db => Handler(db).CreateAsync(CreateCommand(patient.Id, Guid.CreateVersion7(), from, null, true)));

        Assert.True(results.First.Success);
        Assert.Equal("CONFLICT", results.Second.Code);
        await using var verify = fixture.CreatePatientsDbContext();
        var primary = await verify.GuardianLinks.Where(x => x.PatientProfileId == patient.Id && x.IsPrimary).ToListAsync();
        Assert.Single(primary);
        Assert.True(primary[0].AppliesOn(from));
    }

    [Fact]
    public async Task CreateCreate_OverlappingNonPrimaryGuardians_ConfirmsBoth()
    {
        var patient = await AddPatientAsync();
        var from = Today().AddDays(1);

        var results = await RunContendedAsync(
            db => Handler(db).CreateAsync(CreateCommand(patient.Id, Guid.CreateVersion7(), from, null, false)),
            db => Handler(db).CreateAsync(CreateCommand(patient.Id, Guid.CreateVersion7(), from, null, false)));

        Assert.True(results.First.Success);
        Assert.True(results.Second.Success);
        await using var verify = fixture.CreatePatientsDbContext();
        Assert.Equal(2, await verify.GuardianLinks.CountAsync(x => x.PatientProfileId == patient.Id));
    }

    [Fact]
    public async Task CreateEnd_WhenEndOwnsLockFirst_AllowsAdjacentReplacementAndPreservesHistory()
    {
        var patient = await AddPatientAsync();
        var guardian = Guid.CreateVersion7();
        var original = await AddCurrentLinkAsync(patient.Id, guardian);
        var boundary = Today().AddDays(2);

        var results = await RunContendedAsync(
            db => Handler(db).EndAsync(EndCommand(patient.Id, original, boundary)),
            db => Handler(db).CreateAsync(CreateCommand(patient.Id, guardian, boundary, null, false)));

        Assert.True(results.First.Success);
        Assert.True(results.Second.Success);
        await using var verify = fixture.CreatePatientsDbContext();
        var links = await verify.GuardianLinks.Where(x => x.PatientProfileId == patient.Id).OrderBy(x => x.EffectiveFrom).ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.Equal(boundary, links.Single(x => x.Id == original.Id).EffectiveTo);
        Assert.Equal(2, links.Single(x => x.Id == original.Id).Version);
        Assert.Equal(boundary, links.Single(x => x.Id != original.Id).EffectiveFrom);
    }

    [Fact]
    public async Task CreateEnd_WhenCreateOwnsLockFirst_RejectsOverlapThenAllowsEnd()
    {
        var patient = await AddPatientAsync();
        var guardian = Guid.CreateVersion7();
        var original = await AddCurrentLinkAsync(patient.Id, guardian);
        var boundary = Today().AddDays(2);

        var results = await RunContendedAsync(
            db => Handler(db).CreateAsync(CreateCommand(patient.Id, guardian, boundary, null, false)),
            db => Handler(db).EndAsync(EndCommand(patient.Id, original, boundary)));

        Assert.Equal("CONFLICT", results.First.Code);
        Assert.True(results.Second.Success);
        await using var verify = fixture.CreatePatientsDbContext();
        var links = await verify.GuardianLinks.Where(x => x.PatientProfileId == patient.Id).ToListAsync();
        var persisted = Assert.Single(links);
        Assert.Equal(original.Id, persisted.Id);
        Assert.Equal(boundary, persisted.EffectiveTo);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task EndEnd_SameEtag_EndsOnceAndIncrementsVersionOnce()
    {
        var patient = await AddPatientAsync();
        var first = await AddCurrentLinkAsync(patient.Id, Guid.CreateVersion7());
        var boundary = Today().AddDays(2);

        var sameLink = await RunContendedAsync(
            db => Handler(db).EndAsync(EndCommand(patient.Id, first, boundary)),
            db => Handler(db).EndAsync(EndCommand(patient.Id, first, boundary)));

        Assert.True(sameLink.First.Success);
        Assert.Equal(412, sameLink.Second.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", sameLink.Second.Code);

        await using var verify = fixture.CreatePatientsDbContext();
        var persisted = await verify.GuardianLinks.SingleAsync(x => x.Id == first.Id);
        Assert.Equal(boundary, persisted.EffectiveTo);
        Assert.Equal(2, persisted.Version);
        Assert.NotNull(persisted.EndedAt);
    }

    [Fact]
    public async Task EndEnd_ForMinor_RejectsSecondEndAndLeavesContinuousLegalCoverage()
    {
        var patient = await AddPatientAsync(new DateOnly(2015, 1, 1));
        var first = await AddCurrentLinkAsync(patient.Id, Guid.CreateVersion7());
        var second = await AddCurrentLinkAsync(patient.Id, Guid.CreateVersion7());
        var boundary = Today().AddDays(2);

        var coverage = await RunContendedAsync(
            db => Handler(db).EndAsync(EndCommand(patient.Id, first, boundary)),
            db => Handler(db).EndAsync(EndCommand(patient.Id, second, boundary)));

        Assert.True(coverage.First.Success);
        Assert.Equal(422, coverage.Second.StatusCode);
        Assert.Equal("GUARDIAN_COVERAGE_REQUIRED", coverage.Second.Code);
        await using var verify = fixture.CreatePatientsDbContext();
        var links = await verify.GuardianLinks.Where(x => x.PatientProfileId == patient.Id).ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.Single(links, x => x.EffectiveTo is null);
        Assert.All(links.Where(x => x.EffectiveTo is not null), x => Assert.Equal(2, x.Version));
    }

    private async Task<(PatientApplicationResult<GuardianLinkView> First, PatientApplicationResult<GuardianLinkView> Second)> RunContendedAsync(
        Func<PatientsDbContext, Task<PatientApplicationResult<GuardianLinkView>>> firstOperation,
        Func<PatientsDbContext, Task<PatientApplicationResult<GuardianLinkView>>> secondOperation)
    {
        using var timeout = new CancellationTokenSource(Timeout);
        var firstGate = new PostLockGateInterceptor(true);
        var secondGate = new PostLockGateInterceptor(false);
        await using var firstDb = fixture.CreatePatientsDbContext(firstGate);
        await using var secondDb = fixture.CreatePatientsDbContext(secondGate);
        var firstTask = firstOperation(firstDb);
        await firstGate.LockAcquired.WaitAsync(Timeout, timeout.Token);
        var secondTask = secondOperation(secondDb);
        await secondGate.LockAttempted.WaitAsync(Timeout, timeout.Token);
        await WaitForPostgreSqlLockWaiterAsync(timeout.Token);
        firstGate.Release();
        var results = await Task.WhenAll(firstTask, secondTask).WaitAsync(Timeout, timeout.Token);
        Assert.Equal(1, firstGate.LockAcquisitions);
        Assert.Equal(1, secondGate.LockAcquisitions);
        await AssertNoIdleTransactionsAsync();
        return (results[0], results[1]);
    }

    private async Task WaitForPostgreSqlLockWaiterAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.Add(Timeout);
        while (DateTime.UtcNow < deadline)
        {
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%FOR UPDATE%'", connection);
            if ((long)(await command.ExecuteScalarAsync(ct))! > 0) return;
            await Task.Delay(TimeSpan.FromMilliseconds(25), ct);
        }
        throw new TimeoutException("The second GuardianLink transaction did not wait on the PostgreSQL row lock.");
    }

    private async Task AssertNoIdleTransactionsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'idle in transaction'", connection);
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }

    private async Task<PatientProfile> AddPatientAsync(DateOnly? birthDate = null)
    {
        var patient = PatientProfile.Create(Guid.CreateVersion7(), fixture.ActiveUnitId, Today());
        await using var db = fixture.CreatePatientsDbContext();
        db.PatientProfiles.Add(patient);
        await db.SaveChangesAsync();
        patientBirthDates[patient.PersonId] = birthDate ?? new DateOnly(1980, 1, 1);
        return patient;
    }

    private readonly Dictionary<Guid, DateOnly> patientBirthDates = [];

    private async Task<GuardianLink> AddCurrentLinkAsync(Guid patientId, Guid guardianId)
    {
        var link = GuardianLink.Create(patientId, guardianId, Today(), null, false, Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        await using var db = fixture.CreatePatientsDbContext();
        db.GuardianLinks.Add(link);
        await db.SaveChangesAsync();
        return link;
    }

    private ManageGuardianLinks Handler(PatientsDbContext db) => new(db, new FoundPeople(patientBirthDates), NullLogger<ManageGuardianLinks>.Instance);
    private GuardianLinkCommand CreateCommand(Guid patientId, Guid guardianId, DateOnly from, DateOnly? to, bool primary) => new(patientId, guardianId, from, to, primary, Actor());
    private EndGuardianLinkCommand EndCommand(Guid patientId, GuardianLink link, DateOnly to) => new(patientId, link.Id, to, Etag(link), Actor());
    private PatientRequestActor Actor() => new(Guid.CreateVersion7(), true, true, false, new HashSet<string>(["patients.guardian.manage", PeoplePermissions.ReadPerson]), new HashSet<Guid>([fixture.ActiveUnitId]));
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    private static string Etag(GuardianLink link) => $"\"guardian-{link.Id:N}-v{link.Version}\"";

    private sealed class FoundPeople(IReadOnlyDictionary<Guid, DateOnly> birthDates) : IGetPersonForGuardianLink
    {
        public Task<GuardianPersonResult> GetAsync(Guid personId, PeopleActorContext actor, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GuardianPersonResult(GuardianPersonOutcome.Found, new GuardianPersonData(personId, "Test Person", birthDates.TryGetValue(personId, out var birthDate) ? birthDate : new DateOnly(1980, 1, 1))));

        public Task<IReadOnlyDictionary<Guid, GuardianPersonData>?> GetManyAsync(IReadOnlyCollection<Guid> personIds, PeopleActorContext actor, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, GuardianPersonData>?>(personIds.ToDictionary(x => x, x => new GuardianPersonData(x, "Test Person", new DateOnly(1980, 1, 1))));
    }

    private sealed class PostLockGateInterceptor(bool pauseAfterLock) : DbCommandInterceptor
    {
        private readonly TaskCompletionSource lockAcquired = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource lockAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task LockAcquired => lockAcquired.Task;
        public Task LockAttempted => lockAttempted.Task;
        public int LockAcquisitions { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)) lockAttempted.TrySetResult();
            return ValueTask.FromResult(result);
        }

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)) return result;
            LockAcquisitions++;
            lockAcquired.TrySetResult();
            if (pauseAfterLock) await release.Task.WaitAsync(Timeout, cancellationToken);
            return result;
        }

        public void Release() => release.TrySetResult();
    }
}
