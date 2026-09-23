using Fisiofit.ModuleContracts.People;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal sealed record SearchPatientsQuery(
    string? Search,
    string? PrimaryUnitId,
    string? AdministrativeStatus,
    string? Page,
    string? PageSize,
    string? Sort,
    IReadOnlyCollection<string> QueryParameterNames,
    PatientRequestActor Actor,
    string CorrelationId);

internal sealed record PatientListResponse(
    IReadOnlyList<PatientListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

internal sealed record PatientListItem(
    Guid PatientId,
    string FullName,
    PatientCpfView Cpf,
    PatientListPhone PrimaryPhone,
    PatientUnitView PrimaryUnit,
    string AdministrativeStatus,
    DateOnly RelationshipStartedOn);
