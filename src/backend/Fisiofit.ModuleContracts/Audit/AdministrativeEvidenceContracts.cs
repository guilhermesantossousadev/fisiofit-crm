namespace Fisiofit.ModuleContracts.PrivacyAudit;

/// <summary>Closed, purpose-specific boundary for durable administrative audit evidence.</summary>
public interface IRecordAdministrativeEvidence
{
    Task<AdministrativeEvidenceWriteResult> RecordAsync(
        AdministrativeEvidence evidence,
        CancellationToken cancellationToken = default);
}

public sealed record AdministrativeEvidence(
    Guid EvidenceId,
    DateTimeOffset OccurredAt,
    AuditActor Actor,
    AdministrativeAuditAction Action,
    AuditResource Resource,
    AuditResult Result,
    Guid? UnitId,
    string CorrelationId,
    string? TraceId,
    string? ReasonCode,
    AdministrativeEvidenceMetadata? Metadata,
    int SchemaVersion = 1);

public sealed record AuditActor(AuditActorKind Kind, Guid? UserAccountId)
{
    public static AuditActor UserAccount(Guid userAccountId) => new(AuditActorKind.UserAccount, userAccountId);
    public static AuditActor System() => new(AuditActorKind.System, null);
    public static AuditActor Bootstrap() => new(AuditActorKind.Bootstrap, null);
    public static AuditActor BackgroundProcess() => new(AuditActorKind.BackgroundProcess, null);
}

public enum AuditActorKind { UserAccount, System, Bootstrap, BackgroundProcess }

public enum AdministrativeAuditAction
{
    IdentityBootstrapConsumed,
    IdentityAccountCreated,
    IdentityAccountActivated,
    IdentityAccountLocked,
    IdentityAccountDisabled,
    IdentityAccountReactivated,
    IdentityLoginSucceeded,
    IdentitySessionLoggedOut,
    IdentitySessionRevoked,
    IdentityCredentialResetInitiated,
    IdentityCredentialResetCompleted,
    IdentityPermissionGranted,
    IdentityPermissionRevoked,
    IdentityDenyCreated,
    IdentityDenyRemoved,
    IdentityUnitAccessGranted,
    IdentityUnitAccessRevoked
}

public sealed record AuditResource(string Type, Guid Id);
public enum AuditResult { Success, Denied, Failed }

/// <summary>Only the design-approved, non-PII metadata vocabulary is representable.</summary>
public sealed record AdministrativeEvidenceMetadata(
    string? PermissionCode = null,
    string? PreviousStatus = null,
    string? NewStatus = null,
    string? GrantScopeType = null,
    string? SessionScope = null,
    Guid? SessionId = null);

public enum AdministrativeEvidenceWriteDisposition
{
    Recorded,
    AlreadyRecorded,
    Conflict,
    ValidationFailure,
    PersistenceFailure,
    UnknownOutcome
}

public sealed record AdministrativeEvidenceWriteResult(
    AdministrativeEvidenceWriteDisposition Disposition,
    Guid EvidenceId,
    string? FailureCode = null)
{
    public bool Accepted => Disposition is AdministrativeEvidenceWriteDisposition.Recorded or AdministrativeEvidenceWriteDisposition.AlreadyRecorded;
}
