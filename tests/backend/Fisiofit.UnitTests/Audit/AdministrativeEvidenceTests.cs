using Fisiofit.ModuleContracts.PrivacyAudit;
using Fisiofit.Modules.Audit.Application;
using Fisiofit.Modules.Audit.Domain;
using Xunit;

namespace Fisiofit.UnitTests.Audit;

public sealed class AdministrativeEvidenceTests
{
    [Fact]
    public void ValidEvidence_IsAcceptedAtTheBoundary()
    {
        Assert.Null(AdministrativeEvidenceValidator.Validate(ValidEvidence()));
    }

    [Fact]
    public void HumanActor_RequiresOpaqueUserAccountId()
    {
        var evidence = ValidEvidence() with { Actor = new(AuditActorKind.UserAccount, null) };
        Assert.Equal("INVALID_ACTOR", AdministrativeEvidenceValidator.Validate(evidence));
    }

    [Fact]
    public void NonHumanActor_MustNotCarryUserAccountId()
    {
        var evidence = ValidEvidence() with { Actor = new(AuditActorKind.System, Guid.NewGuid()) };
        Assert.Equal("INVALID_ACTOR", AdministrativeEvidenceValidator.Validate(evidence));
    }

    [Fact]
    public void InvalidTimestampOrSchema_IsRejected()
    {
        Assert.Equal("INVALID_OCCURRED_AT", AdministrativeEvidenceValidator.Validate(ValidEvidence() with { OccurredAt = DateTimeOffset.Now }));
        Assert.Equal("UNSUPPORTED_SCHEMA_VERSION", AdministrativeEvidenceValidator.Validate(ValidEvidence() with { SchemaVersion = 2 }));
    }

    [Fact]
    public void UnknownActionOrResult_IsRejected()
    {
        Assert.Equal("INVALID_CATALOG_VALUE", AdministrativeEvidenceValidator.Validate(ValidEvidence() with { Action = (AdministrativeAuditAction)999 }));
        Assert.Equal("INVALID_CATALOG_VALUE", AdministrativeEvidenceValidator.Validate(ValidEvidence() with { Result = (AuditResult)999 }));
    }

    [Fact]
    public void Metadata_IsClosedAndCanonicalForSemanticComparison()
    {
        var evidence = ValidEvidence() with { Metadata = new(PermissionCode: "patients.profile.create", SessionScope: "single") };
        var record = AuditRecord.Create(evidence, DateTimeOffset.UtcNow);
        Assert.True(record.SemanticallyEquals(evidence));
        Assert.False(record.SemanticallyEquals(evidence with { Metadata = new(PermissionCode: "patients.profile.read", SessionScope: "single") }));
        Assert.Equal("INVALID_METADATA", AdministrativeEvidenceValidator.Validate(evidence with { Metadata = new(SessionScope: "global") }));
    }

    [Fact]
    public void SemanticEquality_UsesPostgreSqlTimestampPrecision()
    {
        var evidence = ValidEvidence() with { OccurredAt = new DateTimeOffset(638948736000000009, TimeSpan.Zero) };
        var record = AuditRecord.Create(evidence, DateTimeOffset.UtcNow);

        Assert.True(record.SemanticallyEquals(evidence with { OccurredAt = evidence.OccurredAt.AddTicks(-9) }));
    }

    [Fact]
    public void ProductiveContract_HasNoUnstructuredOrSensitiveInputSurface()
    {
        var names = typeof(AdministrativeEvidence).GetProperties().Select(x => x.Name)
            .Concat(typeof(AdministrativeEvidenceMetadata).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain(names, name => new[] { "Password", "PasswordHash", "Credential", "Cpf", "Email", "Phone", "Address", "Cookie", "Authorization", "Token", "Payload", "StackTrace" }.Contains(name, StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(AdministrativeEvidence).GetProperties(), x => x.PropertyType == typeof(object));
        Assert.DoesNotContain(typeof(AdministrativeEvidence).GetProperties(), x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>));
    }

    private static AdministrativeEvidence ValidEvidence() => new(
        Guid.NewGuid(), DateTimeOffset.UtcNow, AuditActor.UserAccount(Guid.NewGuid()),
        AdministrativeAuditAction.IdentityAccountDisabled, new("UserAccount", Guid.NewGuid()), AuditResult.Success,
        Guid.NewGuid(), "correlation-001", null, null, null);
}
