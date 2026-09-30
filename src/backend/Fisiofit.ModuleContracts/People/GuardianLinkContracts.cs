namespace Fisiofit.ModuleContracts.People;

public enum GuardianPersonOutcome { Found, NotFound, NotCurrent, Forbidden, DependencyFailure }

public sealed record GuardianPersonData(Guid PersonId, string DisplayName, DateOnly BirthDate);

public sealed record GuardianPersonResult(GuardianPersonOutcome Outcome, GuardianPersonData? Data = null);

/// <summary>Purpose-specific, minimized People projection for Patients GuardianLink workflows.</summary>
public interface IGetPersonForGuardianLink
{
    Task<GuardianPersonResult> GetAsync(Guid personId, PeopleActorContext actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, GuardianPersonData>?> GetManyAsync(IReadOnlyCollection<Guid> personIds, PeopleActorContext actor, CancellationToken cancellationToken = default);
}
