using System.Security.Claims;
using System.Text.Json.Serialization;
using Fisiofit.ModuleContracts.People;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal static class PatientPermissions
{
    public const string CreateProfile = "patients.profile.create";
    public const string ReadProfile = "patients.profile.read";
}

internal sealed record PatientRequestActor(
    Guid ActorId,
    bool IsAuthenticated,
    bool IsActive,
    bool HasExplicitDeny,
    IReadOnlySet<string> Permissions,
    IReadOnlySet<Guid> UnitIds)
{
    public PeopleActorContext ToPeopleActor() =>
        new(ActorId, IsAuthenticated, IsActive, HasExplicitDeny, Permissions.ToArray());

    public static PatientRequestActor FromPrincipal(ClaimsPrincipal principal)
    {
        var authenticated = principal.Identity?.IsAuthenticated == true;
        var actorClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorIdValid = Guid.TryParse(actorClaim, out var actorId) && actorId != Guid.Empty;
        var permissions = principal.FindAll("permission")
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);
        var units = principal.FindAll("unit_id")
            .Select(claim => Guid.TryParse(claim.Value, out var unitId) ? unitId : Guid.Empty)
            .Where(unitId => unitId != Guid.Empty)
            .ToHashSet();

        return new(
            actorId,
            authenticated && actorIdValid,
            string.Equals(principal.FindFirstValue("account_status"), "ACTIVE", StringComparison.Ordinal),
            string.Equals(principal.FindFirstValue("explicit_deny"), "true", StringComparison.OrdinalIgnoreCase),
            permissions,
            units);
    }
}

internal sealed record RegisterPatientCommand(
    string? FullName,
    DateOnly BirthDate,
    PatientRegistrationPhone? Phone,
    string? Cpf,
    Guid PrimaryUnitId,
    DateOnly RelationshipStartedOn,
    string? PayerMode,
    string? IdempotencyKey,
    PatientRequestActor Actor,
    string CorrelationId);

internal sealed record RegisterPatientResponse(Guid PatientId, Guid PersonId, string Status, bool Replayed);

internal sealed record PatientApplicationResult<T>(
    bool Success,
    T? Value = default,
    int StatusCode = 200,
    string? Code = null,
    string? Detail = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static PatientApplicationResult<T> Ok(T value) => new(true, value);

    public static PatientApplicationResult<T> Fail(
        int statusCode,
        string code,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(false, StatusCode: statusCode, Code: code, Detail: detail, Errors: errors);
}

internal sealed record PatientAdministrativeDetails(
    Guid PatientId,
    Guid PersonId,
    string FullName,
    DateOnly BirthDate,
    PatientRegistrationPhone Phone,
    PatientCpfView Cpf,
    PatientUnitView PrimaryUnit,
    string AdministrativeStatus,
    DateOnly RelationshipStartedOn);

internal sealed record PatientCpfView(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Masked = null);

internal sealed record PatientUnitView(Guid UnitId, string Name, string Status);
