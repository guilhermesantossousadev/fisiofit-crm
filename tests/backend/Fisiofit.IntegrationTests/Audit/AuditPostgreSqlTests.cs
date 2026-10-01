using Fisiofit.ModuleContracts.PrivacyAudit;
using Fisiofit.Modules.Audit.Application;
using Fisiofit.Modules.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Fisiofit.IntegrationTests.Audit;

public sealed class AuditPostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fisiofit_audit_tests").WithUsername("fisiofit_test").WithPassword("test-only-password").Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await container.DisposeAsync();

    [Fact]
    public async Task Migration_CreatesAuditSchemaWithoutForeignKeys_AndRecordPersists()
    {
        var evidence = Evidence();
        await using var db = CreateDbContext();
        var result = await new RecordAdministrativeEvidence(db).RecordAsync(evidence);
        Assert.Equal(AdministrativeEvidenceWriteDisposition.Recorded, result.Disposition);
        Assert.Equal(1, await db.AuditRecords.CountAsync());
        Assert.Equal("audit", await ScalarAsync<string>("SELECT table_schema FROM information_schema.tables WHERE table_schema = 'audit' AND table_name = 'audit_record'"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM information_schema.table_constraints WHERE table_schema = 'audit' AND table_name = 'audit_record' AND constraint_type = 'FOREIGN KEY'"));
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM audit.\"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20261001000000_Audit_InitialDurableAdministrativeFoundation'"));
        Assert.Equal(8L, await ScalarAsync<long>("SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'audit' AND tablename = 'audit_record'"));
    }

    [Fact]
    public async Task SameEvidence_ReplaysIdempotently_AndChangedContentConflicts()
    {
        var evidence = Evidence();
        await using (var first = CreateDbContext()) Assert.Equal(AdministrativeEvidenceWriteDisposition.Recorded, (await new RecordAdministrativeEvidence(first).RecordAsync(evidence)).Disposition);
        await using (var replay = CreateDbContext()) Assert.Equal(AdministrativeEvidenceWriteDisposition.AlreadyRecorded, (await new RecordAdministrativeEvidence(replay).RecordAsync(evidence)).Disposition);
        await using (var conflict = CreateDbContext()) Assert.Equal(AdministrativeEvidenceWriteDisposition.Conflict, (await new RecordAdministrativeEvidence(conflict).RecordAsync(evidence with { Result = AuditResult.Denied })).Disposition);
        await using var verify = CreateDbContext();
        Assert.Equal(1, await verify.AuditRecords.CountAsync(x => x.EvidenceId == evidence.EvidenceId));
    }

    [Fact]
    public async Task ConcurrentSameEvidence_PersistsExactlyOneRecord()
    {
        var evidence = Evidence();
        await using var first = CreateDbContext();
        await using var second = CreateDbContext();
        var outcomes = await Task.WhenAll(new RecordAdministrativeEvidence(first).RecordAsync(evidence), new RecordAdministrativeEvidence(second).RecordAsync(evidence));
        Assert.Contains(outcomes, x => x.Disposition == AdministrativeEvidenceWriteDisposition.Recorded);
        Assert.Contains(outcomes, x => x.Disposition == AdministrativeEvidenceWriteDisposition.AlreadyRecorded);
        await using var verify = CreateDbContext();
        Assert.Equal(1, await verify.AuditRecords.CountAsync(x => x.EvidenceId == evidence.EvidenceId));
    }

    [Fact]
    public async Task PersistenceFailure_IsExplicit_WhenPostgreSqlIsUnavailable()
    {
        var unavailable = new AuditDbContext(new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unavailable;Username=unused;Password=unused;Timeout=1").Options);
        await using (unavailable)
        {
            var result = await new RecordAdministrativeEvidence(unavailable).RecordAsync(Evidence());
            Assert.Equal(AdministrativeEvidenceWriteDisposition.PersistenceFailure, result.Disposition);
        }
    }

    [Fact]
    public async Task CancelledCommit_IsUnknown_AndSameEvidenceCanBeReconciledWithoutDuplicate()
    {
        var evidence = Evidence();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using (var cancelled = CreateDbContext())
        {
            var unknown = await new RecordAdministrativeEvidence(cancelled).RecordAsync(evidence, cancellation.Token);
            Assert.Equal(AdministrativeEvidenceWriteDisposition.UnknownOutcome, unknown.Disposition);
        }

        await using (var reconciliation = CreateDbContext())
        {
            var result = await new RecordAdministrativeEvidence(reconciliation).RecordAsync(evidence);
            Assert.Equal(AdministrativeEvidenceWriteDisposition.Recorded, result.Disposition);
        }

        await using var verify = CreateDbContext();
        Assert.Equal(1, await verify.AuditRecords.CountAsync(x => x.EvidenceId == evidence.EvidenceId));
    }

    [Fact]
    public async Task AuditRecord_IsAppendOnly()
    {
        await using var db = CreateDbContext();
        var evidence = Evidence();
        Assert.Equal(AdministrativeEvidenceWriteDisposition.Recorded, (await new RecordAdministrativeEvidence(db).RecordAsync(evidence)).Disposition);
        var record = await db.AuditRecords.SingleAsync(x => x.EvidenceId == evidence.EvidenceId);
        db.Remove(record);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    private AuditDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(container.GetConnectionString(),
        npgsql => npgsql.MigrationsHistoryTable(AuditDbContext.MigrationsHistoryTable, AuditDbContext.Schema)).Options);

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new Npgsql.NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static AdministrativeEvidence Evidence() => new(Guid.NewGuid(), DateTimeOffset.UtcNow, AuditActor.UserAccount(Guid.NewGuid()), AdministrativeAuditAction.IdentityAccountDisabled, new("UserAccount", Guid.NewGuid()), AuditResult.Success, Guid.NewGuid(), "audit-test-correlation", null, null, new(NewStatus: "DISABLED"));
}
