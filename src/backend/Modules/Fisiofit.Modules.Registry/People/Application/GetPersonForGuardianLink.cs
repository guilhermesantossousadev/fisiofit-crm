using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.People.Domain;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.People.Application;

internal sealed class GetPersonForGuardianLink(PeopleDbContext dbContext) : IGetPersonForGuardianLink
{
    public async Task<GuardianPersonResult> GetAsync(Guid personId, PeopleActorContext actor, CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor)) return new(GuardianPersonOutcome.Forbidden);
        var person = await dbContext.People.AsNoTracking().Where(x => x.Id == personId)
            .Select(x => new { x.Id, x.FullName, x.BirthDate, x.RecordState }).SingleOrDefaultAsync(cancellationToken);
        if (person is null) return new(GuardianPersonOutcome.NotFound);
        if (person.RecordState != PersonRecordState.Current) return new(GuardianPersonOutcome.NotCurrent);
        return new(GuardianPersonOutcome.Found, new(person.Id, person.FullName, person.BirthDate));
    }

    public async Task<IReadOnlyDictionary<Guid, GuardianPersonData>?> GetManyAsync(IReadOnlyCollection<Guid> personIds, PeopleActorContext actor, CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor) || personIds.Any(x => x == Guid.Empty)) return null;
        var rows = await dbContext.People.AsNoTracking().Where(x => personIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName, x.BirthDate }).ToListAsync(cancellationToken);
        return rows.Count != personIds.Count ? null : rows.ToDictionary(x => x.Id, x => new GuardianPersonData(x.Id, x.FullName, x.BirthDate));
    }

    private static bool Authorized(PeopleActorContext actor) => actor.IsAuthenticated && actor.IsActive && !actor.HasExplicitDeny && actor.Permissions.Contains(PeoplePermissions.ReadPerson, StringComparer.Ordinal);
}
