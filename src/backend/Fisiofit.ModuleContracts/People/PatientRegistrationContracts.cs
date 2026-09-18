namespace Fisiofit.ModuleContracts.People;

public static class PeoplePermissions
{
    public const string CreatePerson = "people.person.create";
    public const string ReadPerson = "people.person.read";
}

public sealed record PeopleActorContext(
    Guid ActorId,
    bool IsAuthenticated,
    bool IsActive,
    bool HasExplicitDeny,
    IReadOnlyCollection<string> Permissions);

public sealed record PatientRegistrationPhone(string CountryCode, string AreaCode, string Number);

public sealed record CreatePersonForPatientRegistrationRequest(
    string FullName,
    DateOnly BirthDate,
    PatientRegistrationPhone Phone,
    string? Cpf,
    string OperationKey,
    string RequestHash,
    PeopleActorContext Actor,
    string CorrelationId);

public enum CreatePersonForPatientRegistrationOutcome
{
    Created,
    Replayed,
    CpfConflict,
    ValidationFailure,
    Forbidden,
    OperationConflict,
    DependencyFailure
}

public sealed record CreatePersonForPatientRegistrationResult(
    CreatePersonForPatientRegistrationOutcome Outcome,
    Guid? PersonId = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public interface ICreatePersonForPatientRegistration
{
    Task<CreatePersonForPatientRegistrationResult> ExecuteAsync(
        CreatePersonForPatientRegistrationRequest request,
        CancellationToken cancellationToken = default);
}

public enum GetPersonPatientRegistrationDataOutcome
{
    Found,
    NotFound,
    Forbidden,
    DependencyFailure
}

public sealed record PersonPatientRegistrationData(
    Guid PersonId,
    string FullName,
    DateOnly BirthDate,
    PatientRegistrationPhone PrimaryPhone,
    string CpfStatus,
    string? MaskedCpf);

public sealed record GetPersonPatientRegistrationDataResult(
    GetPersonPatientRegistrationDataOutcome Outcome,
    PersonPatientRegistrationData? Data = null);

public interface IGetPersonPatientRegistrationData
{
    Task<GetPersonPatientRegistrationDataResult> GetAsync(
        Guid personId,
        PeopleActorContext actor,
        CancellationToken cancellationToken = default);
}
