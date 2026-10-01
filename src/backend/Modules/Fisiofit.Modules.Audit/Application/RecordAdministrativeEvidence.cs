using Fisiofit.ModuleContracts.PrivacyAudit;
using Fisiofit.Modules.Audit.Domain;
using Fisiofit.Modules.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fisiofit.Modules.Audit.Application;

internal sealed class RecordAdministrativeEvidence(AuditDbContext dbContext) : IRecordAdministrativeEvidence
{
    public async Task<AdministrativeEvidenceWriteResult> RecordAsync(AdministrativeEvidence evidence, CancellationToken cancellationToken = default)
    {
        var failure = AdministrativeEvidenceValidator.Validate(evidence);
        if (failure is not null) return new(AdministrativeEvidenceWriteDisposition.ValidationFailure, evidence.EvidenceId, failure);

        dbContext.AuditRecords.Add(AuditRecord.Create(evidence, DateTimeOffset.UtcNow));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(AdministrativeEvidenceWriteDisposition.Recorded, evidence.EvidenceId);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.AuditRecords.SingleOrDefaultAsync(x => x.EvidenceId == evidence.EvidenceId, cancellationToken);
            return existing is not null && existing.SemanticallyEquals(evidence)
                ? new(AdministrativeEvidenceWriteDisposition.AlreadyRecorded, evidence.EvidenceId)
                : new(AdministrativeEvidenceWriteDisposition.Conflict, evidence.EvidenceId, "EVIDENCE_ID_CONFLICT");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(AdministrativeEvidenceWriteDisposition.UnknownOutcome, evidence.EvidenceId, "COMMIT_OUTCOME_UNKNOWN");
        }
        catch (DbUpdateException)
        {
            return new(AdministrativeEvidenceWriteDisposition.UnknownOutcome, evidence.EvidenceId, "COMMIT_OUTCOME_UNKNOWN");
        }
        catch (InvalidOperationException exception) when (exception.InnerException is NpgsqlException)
        {
            return new(AdministrativeEvidenceWriteDisposition.PersistenceFailure, evidence.EvidenceId, "AUDIT_PERSISTENCE_FAILURE");
        }
        catch (NpgsqlException)
        {
            return new(AdministrativeEvidenceWriteDisposition.PersistenceFailure, evidence.EvidenceId, "AUDIT_PERSISTENCE_FAILURE");
        }
    }
}

internal static class AdministrativeEvidenceValidator
{
    internal static string? Validate(AdministrativeEvidence evidence)
    {
        if (evidence.EvidenceId == Guid.Empty || evidence.Resource.Id == Guid.Empty) return "INVALID_IDENTIFIER";
        if (evidence.OccurredAt.Offset != TimeSpan.Zero || evidence.OccurredAt > DateTimeOffset.UtcNow.AddMinutes(5)) return "INVALID_OCCURRED_AT";
        if (evidence.SchemaVersion != 1) return "UNSUPPORTED_SCHEMA_VERSION";
        if (string.IsNullOrWhiteSpace(evidence.Resource.Type) || evidence.Resource.Type.Length > 64) return "INVALID_RESOURCE";
        if (string.IsNullOrWhiteSpace(evidence.CorrelationId) || evidence.CorrelationId.Length > 128 || evidence.TraceId?.Length > 128 || evidence.ReasonCode?.Length > 64) return "INVALID_CORRELATION_OR_REASON";
        if (evidence.Actor.Kind == AuditActorKind.UserAccount && (evidence.Actor.UserAccountId is not { } userAccountId || userAccountId == Guid.Empty)) return "INVALID_ACTOR";
        if (evidence.Actor.Kind != AuditActorKind.UserAccount && evidence.Actor.UserAccountId is not null) return "INVALID_ACTOR";
        if (!Enum.IsDefined(evidence.Action) || !Enum.IsDefined(evidence.Result)) return "INVALID_CATALOG_VALUE";
        return ValidateMetadata(evidence.Metadata);
    }

    private static string? ValidateMetadata(AdministrativeEvidenceMetadata? metadata)
    {
        if (metadata is null) return null;
        if (metadata.PermissionCode?.Length > 96 || metadata.PreviousStatus?.Length > 32 || metadata.NewStatus?.Length > 32 || metadata.GrantScopeType?.Length > 32 || metadata.SessionScope?.Length > 16) return "INVALID_METADATA";
        if (metadata.SessionScope is not null && metadata.SessionScope is not ("single" or "all")) return "INVALID_METADATA";
        return null;
    }
}
