using Fisiofit.ModuleContracts.PrivacyAudit;
using System.Text.Json;

namespace Fisiofit.Modules.Audit.Domain;

internal sealed class AuditRecord
{
    private AuditRecord() { }

    private AuditRecord(AdministrativeEvidence evidence, DateTimeOffset recordedAt)
    {
        AuditRecordId = Guid.CreateVersion7();
        EvidenceId = evidence.EvidenceId;
        OccurredAt = evidence.OccurredAt;
        RecordedAt = recordedAt;
        ActorKind = evidence.Actor.Kind;
        ActorUserAccountId = evidence.Actor.UserAccountId;
        Action = evidence.Action.ToCode();
        ResourceType = evidence.Resource.Type;
        ResourceId = evidence.Resource.Id;
        Result = evidence.Result;
        UnitId = evidence.UnitId;
        CorrelationId = evidence.CorrelationId;
        TraceId = evidence.TraceId;
        ReasonCode = evidence.ReasonCode;
        Metadata = evidence.Metadata?.ToCanonicalValue();
        SchemaVersion = evidence.SchemaVersion;
    }

    public Guid AuditRecordId { get; private set; }
    public Guid EvidenceId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public AuditActorKind ActorKind { get; private set; }
    public Guid? ActorUserAccountId { get; private set; }
    public string Action { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public AuditResult Result { get; private set; }
    public Guid? UnitId { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public string? TraceId { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Metadata { get; private set; }
    public int SchemaVersion { get; private set; }

    public static AuditRecord Create(AdministrativeEvidence evidence, DateTimeOffset recordedAt) => new(evidence, recordedAt);
    public bool SemanticallyEquals(AdministrativeEvidence evidence) =>
        EvidenceId == evidence.EvidenceId && CanonicalOccurredAt(OccurredAt) == CanonicalOccurredAt(evidence.OccurredAt) && ActorKind == evidence.Actor.Kind &&
        ActorUserAccountId == evidence.Actor.UserAccountId && Action == evidence.Action.ToCode() &&
        ResourceType == evidence.Resource.Type && ResourceId == evidence.Resource.Id && Result == evidence.Result &&
        UnitId == evidence.UnitId && CorrelationId == evidence.CorrelationId && TraceId == evidence.TraceId &&
        ReasonCode == evidence.ReasonCode && MetadataEquals(evidence.Metadata) && SchemaVersion == evidence.SchemaVersion;

    // PostgreSQL timestamps retain microsecond precision; evidence identity must use the
    // same persisted precision so a byte-for-byte replay is not classified as a conflict.
    private static DateTimeOffset CanonicalOccurredAt(DateTimeOffset value)
    {
        var utcTicks = value.UtcDateTime.Ticks;
        return new DateTimeOffset(utcTicks - utcTicks % 10, TimeSpan.Zero);
    }

    private bool MetadataEquals(AdministrativeEvidenceMetadata? metadata)
    {
        if (Metadata is null || metadata is null) return Metadata is null && metadata is null;
        return JsonSerializer.Deserialize<AdministrativeEvidenceMetadata>(Metadata) == metadata;
    }
}

internal static class AuditContractExtensions
{
    public static string ToCode(this AdministrativeAuditAction action) => action switch
    {
        AdministrativeAuditAction.IdentityBootstrapConsumed => "identity.bootstrap.consumed",
        AdministrativeAuditAction.IdentityAccountCreated => "identity.account.created",
        AdministrativeAuditAction.IdentityAccountActivated => "identity.account.activated",
        AdministrativeAuditAction.IdentityAccountLocked => "identity.account.locked",
        AdministrativeAuditAction.IdentityAccountDisabled => "identity.account.disabled",
        AdministrativeAuditAction.IdentityAccountReactivated => "identity.account.reactivated",
        AdministrativeAuditAction.IdentityLoginSucceeded => "identity.login.succeeded",
        AdministrativeAuditAction.IdentitySessionLoggedOut => "identity.session.logged_out",
        AdministrativeAuditAction.IdentitySessionRevoked => "identity.session.revoked",
        AdministrativeAuditAction.IdentityCredentialResetInitiated => "identity.credential.reset_initiated",
        AdministrativeAuditAction.IdentityCredentialResetCompleted => "identity.credential.reset_completed",
        AdministrativeAuditAction.IdentityPermissionGranted => "identity.permission.granted",
        AdministrativeAuditAction.IdentityPermissionRevoked => "identity.permission.revoked",
        AdministrativeAuditAction.IdentityDenyCreated => "identity.deny.created",
        AdministrativeAuditAction.IdentityDenyRemoved => "identity.deny.removed",
        AdministrativeAuditAction.IdentityUnitAccessGranted => "identity.unit_access.granted",
        AdministrativeAuditAction.IdentityUnitAccessRevoked => "identity.unit_access.revoked",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static string ToCanonicalValue(this AdministrativeEvidenceMetadata metadata) => JsonSerializer.Serialize(metadata);
}
