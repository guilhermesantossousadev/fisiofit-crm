using Fisiofit.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Audit.Infrastructure;

internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    internal const string Schema = "audit";
    internal const string MigrationsHistoryTable = "__EFMigrationsHistory";

    internal DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<AuditRecord>();
        record.ToTable("audit_record", Schema);
        record.HasKey(x => x.AuditRecordId).HasName("pk_audit_record");
        record.Property(x => x.AuditRecordId).HasColumnName("audit_record_id");
        record.Property(x => x.EvidenceId).HasColumnName("evidence_id");
        record.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        record.Property(x => x.RecordedAt).HasColumnName("recorded_at");
        record.Property(x => x.ActorKind).HasColumnName("actor_kind").HasConversion<string>().HasMaxLength(32);
        record.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
        record.Property(x => x.Action).HasColumnName("action").HasMaxLength(96);
        record.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(64);
        record.Property(x => x.ResourceId).HasColumnName("resource_id");
        record.Property(x => x.Result).HasColumnName("result").HasConversion<string>().HasMaxLength(16);
        record.Property(x => x.UnitId).HasColumnName("unit_id");
        record.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        record.Property(x => x.TraceId).HasColumnName("trace_id").HasMaxLength(128);
        record.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(64);
        record.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        record.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        record.HasIndex(x => x.EvidenceId).IsUnique().HasDatabaseName("ux_audit_record_evidence_id");
        record.HasIndex(x => new { x.ActorUserAccountId, x.OccurredAt }).HasDatabaseName("ix_audit_record_actor_occurred_at");
        record.HasIndex(x => new { x.Action, x.OccurredAt }).HasDatabaseName("ix_audit_record_action_occurred_at");
        record.HasIndex(x => new { x.ResourceType, x.ResourceId, x.OccurredAt }).HasDatabaseName("ix_audit_record_resource_occurred_at");
        record.HasIndex(x => new { x.UnitId, x.OccurredAt }).HasFilter("unit_id IS NOT NULL").HasDatabaseName("ix_audit_record_unit_occurred_at");
        record.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_audit_record_correlation_id");
        record.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_audit_record_occurred_at");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectMutations();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void RejectMutations()
    {
        if (ChangeTracker.Entries<AuditRecord>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("AuditRecord is append-only.");
        }
    }
}
