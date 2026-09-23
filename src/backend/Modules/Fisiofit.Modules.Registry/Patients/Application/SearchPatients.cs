using Fisiofit.ModuleContracts.Organization;
using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal sealed class SearchPatients(
    PatientsDbContext dbContext,
    ISearchPeopleForPatientList peopleSearch,
    IGetUnitForPatientRead unitReader)
{
    private static readonly HashSet<string> AllowedParameters =
        ["search", "primaryUnitId", "administrativeStatus", "page", "pageSize", "sort"];

    public async Task<PatientApplicationResult<PatientListResponse>> ExecuteAsync(
        SearchPatientsQuery query,
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = Authorize(query.Actor);
        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        var prepared = Prepare(query);
        if (prepared.Error is not null)
        {
            return prepared.Error;
        }

        var request = prepared.Request!;
        if (request.PrimaryUnitId is { } requestedUnit && !query.Actor.UnitIds.Contains(requestedUnit))
        {
            return Forbidden("The requested Unit is outside the actor scope.");
        }

        var effectiveUnitIds = request.PrimaryUnitId is { } unitId
            ? new[] { unitId }
            : query.Actor.UnitIds.ToArray();

        var candidatesQuery = dbContext.PatientProfiles
            .AsNoTracking()
            .Where(profile => effectiveUnitIds.Contains(profile.PrimaryUnitId));
        if (request.Status is { } status)
        {
            candidatesQuery = candidatesQuery.Where(profile => profile.AdministrativeStatus == status);
        }

        var candidates = await candidatesQuery
            .Select(profile => new CandidateProfile(
                profile.Id,
                profile.PersonId,
                profile.PrimaryUnitId,
                profile.AdministrativeStatus,
                profile.RelationshipStartedOn))
            .ToListAsync(cancellationToken);

        var peopleResult = await peopleSearch.ExecuteAsync(
            new SearchPeopleForPatientListRequest(
                candidates.Select(candidate => candidate.PersonId).Distinct().ToArray(),
                request.Search,
                request.Page,
                request.PageSize,
                request.Sort,
                query.Actor.ToPeopleActor(),
                query.CorrelationId),
            cancellationToken);

        if (peopleResult.Outcome != SearchPeopleForPatientListOutcome.Found)
        {
            return MapPeopleFailure(peopleResult);
        }

        var peopleItems = peopleResult.Items ?? [];
        var profilesByPerson = candidates.ToDictionary(candidate => candidate.PersonId);
        var units = new Dictionary<Guid, PatientUnitView>();
        foreach (var unit in peopleItems
                     .Select(person => profilesByPerson[person.PersonId].PrimaryUnitId)
                     .Distinct())
        {
            var unitResult = await unitReader.GetAsync(unit, cancellationToken);
            if (!unitResult.Found)
            {
                return InternalInconsistency();
            }

            units.Add(unit, new PatientUnitView(unitResult.UnitId, unitResult.Name!, unitResult.Status!));
        }

        var items = new List<PatientListItem>(peopleItems.Count);
        foreach (var person in peopleItems)
        {
            if (!profilesByPerson.TryGetValue(person.PersonId, out var profile))
            {
                return InternalInconsistency();
            }

            items.Add(new PatientListItem(
                profile.PatientId,
                person.FullName,
                new PatientCpfView(person.CpfStatus, person.MaskedCpf),
                person.PrimaryPhone,
                units[profile.PrimaryUnitId],
                profile.Status == PatientAdministrativeStatus.Active ? "ACTIVE" : "INACTIVE",
                profile.RelationshipStartedOn));
        }

        return PatientApplicationResult<PatientListResponse>.Ok(
            new PatientListResponse(items, peopleResult.Page, peopleResult.PageSize, peopleResult.TotalCount));
    }

    private static PatientApplicationResult<PatientListResponse>? Authorize(PatientRequestActor actor)
    {
        if (!actor.IsAuthenticated)
        {
            return PatientApplicationResult<PatientListResponse>.Fail(401, "UNAUTHORIZED", "Authentication is required.");
        }

        if (!actor.IsActive
            || actor.HasExplicitDeny
            || !actor.Permissions.Contains(PatientPermissions.ReadProfile)
            || !actor.Permissions.Contains(PeoplePermissions.ReadPerson)
            || actor.UnitIds.Count == 0)
        {
            return Forbidden("The operation is not permitted.");
        }

        return null;
    }

    private static PreparedQuery Prepare(SearchPatientsQuery query)
    {
        var unknown = query.QueryParameterNames.FirstOrDefault(name => !AllowedParameters.Contains(name));
        if (unknown is not null)
        {
            return PreparedQuery.Invalid(unknown, "The query parameter is not supported.");
        }

        Guid? unitId = null;
        if (query.PrimaryUnitId is not null)
        {
            if (!Guid.TryParse(query.PrimaryUnitId, out var parsedUnit) || parsedUnit == Guid.Empty)
            {
                return PreparedQuery.Invalid("primaryUnitId", "Primary Unit must be a non-empty UUID.");
            }

            unitId = parsedUnit;
        }

        PatientAdministrativeStatus? status = null;
        if (query.AdministrativeStatus is not null)
        {
            status = query.AdministrativeStatus switch
            {
                "ACTIVE" => PatientAdministrativeStatus.Active,
                "INACTIVE" => PatientAdministrativeStatus.Inactive,
                _ => null
            };
            if (status is null)
            {
                return PreparedQuery.Invalid("administrativeStatus", "Administrative status must be ACTIVE or INACTIVE.");
            }
        }

        if (!TryPositiveInteger(query.Page, 1, out var page))
        {
            return PreparedQuery.Invalid("page", "Page must be an integer greater than or equal to 1.");
        }

        if (!TryPositiveInteger(query.PageSize, 25, out var pageSize) || pageSize > 100)
        {
            return PreparedQuery.Invalid("pageSize", "Page size must be an integer between 1 and 100.");
        }

        if ((long)(page - 1) * pageSize > int.MaxValue)
        {
            return PreparedQuery.Invalid("page", "The requested page offset is too large.");
        }

        var sort = query.Sort ?? "name";
        if (sort is not ("name" or "-name"))
        {
            return PreparedQuery.Invalid("sort", "Sort must be name or -name.");
        }

        return new(new PreparedPatientSearch(query.Search, unitId, status, page, pageSize, sort));
    }

    private static bool TryPositiveInteger(string? value, int defaultValue, out int parsed)
    {
        if (value is null)
        {
            parsed = defaultValue;
            return true;
        }

        return int.TryParse(
            value,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out parsed) && parsed >= 1;
    }

    private static PatientApplicationResult<PatientListResponse> MapPeopleFailure(
        SearchPeopleForPatientListResult result) =>
        result.Outcome switch
        {
            SearchPeopleForPatientListOutcome.ValidationFailure =>
                PatientApplicationResult<PatientListResponse>.Fail(
                    400, "VALIDATION_ERROR", "The request is invalid.", result.Errors),
            SearchPeopleForPatientListOutcome.Forbidden => Forbidden("The operation is not permitted."),
            _ => InternalInconsistency()
        };

    private static PatientApplicationResult<PatientListResponse> Forbidden(string detail) =>
        PatientApplicationResult<PatientListResponse>.Fail(403, "FORBIDDEN", detail);

    private static PatientApplicationResult<PatientListResponse> InternalInconsistency() =>
        PatientApplicationResult<PatientListResponse>.Fail(500, "INTERNAL_ERROR", "A referenced record is inconsistent.");

    private sealed record CandidateProfile(
        Guid PatientId,
        Guid PersonId,
        Guid PrimaryUnitId,
        PatientAdministrativeStatus Status,
        DateOnly RelationshipStartedOn);

    private sealed record PreparedPatientSearch(
        string? Search,
        Guid? PrimaryUnitId,
        PatientAdministrativeStatus? Status,
        int Page,
        int PageSize,
        string Sort);

    private sealed record PreparedQuery(
        PreparedPatientSearch? Request = null,
        PatientApplicationResult<PatientListResponse>? Error = null)
    {
        public static PreparedQuery Invalid(string field, string message) => new(
            Error: PatientApplicationResult<PatientListResponse>.Fail(
                400,
                "VALIDATION_ERROR",
                "The request is invalid.",
                new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] }));
    }
}
