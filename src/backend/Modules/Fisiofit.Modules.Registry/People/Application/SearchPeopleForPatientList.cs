using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.People.Domain;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.People.Application;

internal sealed class SearchPeopleForPatientList(PeopleDbContext dbContext)
    : ISearchPeopleForPatientList
{
    public async Task<SearchPeopleForPatientListResult> ExecuteAsync(
        SearchPeopleForPatientListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized(request.Actor))
        {
            return new(SearchPeopleForPatientListOutcome.Forbidden);
        }

        var prepared = Prepare(request);
        if (prepared.Error is not null)
        {
            return prepared.Error;
        }

        var personIds = request.EligiblePersonIds.ToArray();
        if (personIds.Length == 0)
        {
            return Found([], request, prepared.Mode, 0);
        }

        var baseQuery = dbContext.People
            .AsNoTracking()
            .Where(person => personIds.Contains(person.Id)
                && person.RecordState == PersonRecordState.Current
                && person.ContactPoints.Any(contact =>
                    contact.Kind == "PHONE"
                    && contact.IsPrimary
                    && contact.Status == ContactPointStatus.Active));

        var consistentCount = await baseQuery.CountAsync(cancellationToken);
        if (consistentCount != personIds.Length)
        {
            return new(SearchPeopleForPatientListOutcome.DependencyFailure);
        }

        var filtered = ApplySearch(baseQuery, prepared);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var descending = string.Equals(request.Sort, "-name", StringComparison.Ordinal);
        var ordered = descending
            ? filtered.OrderByDescending(person => person.FullName.ToLower()).ThenByDescending(person => person.Id)
            : filtered.OrderBy(person => person.FullName.ToLower()).ThenBy(person => person.Id);

        var offset = (request.Page - 1) * request.PageSize;
        var rows = await ordered
            .Skip(offset)
            .Take(request.PageSize)
            .Select(person => new
            {
                person.Id,
                person.FullName,
                person.CpfNormalized,
                Phone = person.ContactPoints
                    .Where(contact => contact.Kind == "PHONE"
                        && contact.IsPrimary
                        && contact.Status == ContactPointStatus.Active)
                    .Select(contact => new { contact.CountryCode, contact.AreaCode, contact.Number })
                    .Single()
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => new PersonForPatientList(
            row.Id,
            row.FullName,
            row.CpfNormalized is null ? "ABSENT" : "PRESENT",
            row.CpfNormalized is null ? null : $"***.***.***-{row.CpfNormalized[^2..]}",
            new PatientListPhone(
                row.Phone.CountryCode,
                row.Phone.AreaCode,
                MaskPhone(row.Phone.Number))))
            .ToArray();

        return Found(items, request, prepared.Mode, totalCount);
    }

    internal static PreparedSearch Prepare(SearchPeopleForPatientListRequest request)
    {
        if (request.EligiblePersonIds.Any(id => id == Guid.Empty)
            || request.EligiblePersonIds.Distinct().Count() != request.EligiblePersonIds.Count)
        {
            return PreparedSearch.Invalid("eligiblePersonIds", "Eligible Person identifiers must be non-empty and unique.");
        }

        if (request.Page < 1
            || request.PageSize is < 1 or > 100
            || (long)(request.Page - 1) * request.PageSize > int.MaxValue)
        {
            return PreparedSearch.Invalid("pagination", "Page and pageSize are outside the supported range.");
        }

        if (request.Sort is not ("name" or "-name"))
        {
            return PreparedSearch.Invalid("sort", "Sort must be name or -name.");
        }

        var search = PeopleNormalization.NormalizeName(request.Search);
        if (search.Length == 0)
        {
            return new(search, "NONE");
        }

        if (search.Length > 100)
        {
            return PreparedSearch.Invalid("search", "Search must contain at most 100 characters.");
        }

        if (search.Any(char.IsLetter))
        {
            return search.Length < 3
                ? PreparedSearch.Invalid("search", "Name search must contain at least 3 characters.")
                : new(search, "NAME");
        }

        if (search.Any(character => !char.IsAsciiDigit(character)
                && character is not ('+' or ' ' or '(' or ')' or '-' or '.')))
        {
            return PreparedSearch.Invalid("search", "Search is not a supported name, CPF or international phone.");
        }

        var digits = PeopleNormalization.Digits(search);
        var cpf = digits.Length == 11 && PeopleNormalization.IsValidCpf(digits);
        var phone = digits.Length is >= 8 and <= 15 && !IsFormattedCpf(search, cpf);
        if (!cpf && !phone)
        {
            return PreparedSearch.Invalid("search", "Search is not a complete valid CPF or international phone.");
        }

        return new(digits, cpf && phone ? "CPF_OR_PHONE" : cpf ? "CPF" : "PHONE", cpf, phone);
    }

    private static IQueryable<Person> ApplySearch(IQueryable<Person> query, PreparedSearch prepared) =>
        prepared.Mode switch
        {
            "NONE" => query,
            "NAME" => query.Where(person => EF.Functions.ILike(
                person.FullName,
                $"%{EscapeLikePattern(prepared.Value)}%",
                "\\")),
            "CPF" => query.Where(person => person.CpfNormalized == prepared.Value),
            "PHONE" => query.Where(person => person.ContactPoints.Any(contact =>
                contact.Kind == "PHONE" && contact.IsPrimary && contact.Status == ContactPointStatus.Active
                && contact.NormalizedValue == $"+{prepared.Value}")),
            "CPF_OR_PHONE" => query.Where(person => person.CpfNormalized == prepared.Value
                || person.ContactPoints.Any(contact =>
                    contact.Kind == "PHONE" && contact.IsPrimary && contact.Status == ContactPointStatus.Active
                    && contact.NormalizedValue == $"+{prepared.Value}")),
            _ => throw new InvalidOperationException("Unsupported prepared search mode.")
        };

    private static bool IsFormattedCpf(string value, bool validCpf) =>
        validCpf
        && value.Length == 14
        && value[3] == '.'
        && value[7] == '.'
        && value[11] == '-';

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static bool IsAuthorized(PeopleActorContext actor) =>
        actor.IsAuthenticated
        && actor.IsActive
        && !actor.HasExplicitDeny
        && actor.Permissions.Contains(PeoplePermissions.ReadPerson, StringComparer.Ordinal);

    private static string MaskPhone(string number) =>
        new string('*', Math.Max(0, number.Length - 4)) + number[^Math.Min(4, number.Length)..];

    private static SearchPeopleForPatientListResult Found(
        IReadOnlyList<PersonForPatientList> items,
        SearchPeopleForPatientListRequest request,
        string mode,
        int totalCount) =>
        new(SearchPeopleForPatientListOutcome.Found, items, request.Page, request.PageSize, totalCount, mode);

    internal sealed record PreparedSearch(
        string Value,
        string Mode,
        bool Cpf = false,
        bool Phone = false,
        SearchPeopleForPatientListResult? Error = null)
    {
        public static PreparedSearch Invalid(string field, string message) => new(
            string.Empty,
            string.Empty,
            Error: new SearchPeopleForPatientListResult(
                SearchPeopleForPatientListOutcome.ValidationFailure,
                Errors: new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] }));
    }
}
