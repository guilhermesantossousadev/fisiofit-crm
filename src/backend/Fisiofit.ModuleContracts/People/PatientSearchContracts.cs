namespace Fisiofit.ModuleContracts.People;

public sealed record SearchPeopleForPatientListRequest(
    IReadOnlyCollection<Guid> EligiblePersonIds,
    string? Search,
    int Page,
    int PageSize,
    string Sort,
    PeopleActorContext Actor,
    string CorrelationId);

public enum SearchPeopleForPatientListOutcome
{
    Found,
    ValidationFailure,
    Forbidden,
    DependencyFailure
}

public sealed record PatientListPhone(string CountryCode, string AreaCode, string MaskedNumber);

public sealed record PersonForPatientList(
    Guid PersonId,
    string FullName,
    string CpfStatus,
    string? MaskedCpf,
    PatientListPhone PrimaryPhone);

public sealed record SearchPeopleForPatientListResult(
    SearchPeopleForPatientListOutcome Outcome,
    IReadOnlyList<PersonForPatientList>? Items = null,
    int Page = 0,
    int PageSize = 0,
    int TotalCount = 0,
    string? SearchMode = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public interface ISearchPeopleForPatientList
{
    Task<SearchPeopleForPatientListResult> ExecuteAsync(
        SearchPeopleForPatientListRequest request,
        CancellationToken cancellationToken = default);
}
