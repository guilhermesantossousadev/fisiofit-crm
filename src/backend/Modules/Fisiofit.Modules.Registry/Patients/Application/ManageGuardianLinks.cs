using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal sealed record GuardianLinkCommand(Guid PatientId, Guid GuardianPersonId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsPrimary, PatientRequestActor Actor);
internal sealed record EndGuardianLinkCommand(Guid PatientId, Guid GuardianLinkId, DateOnly EffectiveTo, string? IfMatch, PatientRequestActor Actor);
internal sealed record GuardianLinkView(Guid GuardianLinkId, Guid PatientId, Guid GuardianPersonId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsPrimary, int Version, string Etag);
internal sealed record GuardianRelationshipView(Guid GuardianLinkId, Guid PatientId, Guid GuardianPersonId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsPrimary, int Version, string Etag, string Kind, string TemporalState, GuardianDisplayName Guardian);
internal sealed record GuardianDisplayName(string DisplayName);
internal sealed record GuardianRelationshipsResponse(IReadOnlyList<GuardianRelationshipView> Items, int Page, int PageSize, int TotalCount);

internal sealed class ManageGuardianLinks(PatientsDbContext db, IGetPersonForGuardianLink people, ILogger<ManageGuardianLinks> logger)
{
    private const string ManagePermission = "patients.guardian.manage";
    private const int MaxTransactionAttempts = 2;

    public async Task<PatientApplicationResult<GuardianLinkView>> CreateAsync(GuardianLinkCommand command, CancellationToken ct = default)
    {
        var auth = Authorize<GuardianLinkView>(command.Actor, ManagePermission); if (auth is not null) return auth;
        if (command.PatientId == Guid.Empty || command.GuardianPersonId == Guid.Empty) return Invalid<GuardianLinkView>();
        var today = Today();
        if (command.EffectiveFrom < today || (command.EffectiveTo is not null && command.EffectiveTo <= command.EffectiveFrom)) return PeriodFailure<GuardianLinkView>(command.EffectiveFrom < today);
        var profile = await db.PatientProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == command.PatientId, ct);
        if (profile is null || !command.Actor.UnitIds.Contains(profile.PrimaryUnitId)) return NotFound<GuardianLinkView>();
        if (profile.AdministrativeStatus != PatientAdministrativeStatus.Active) return StateFailure<GuardianLinkView>();
        if (profile.PersonId == command.GuardianPersonId) return BusinessFailure<GuardianLinkView>("The guardian must differ from the Patient.");
        var person = await people.GetAsync(command.GuardianPersonId, command.Actor.ToPeopleActor(), ct);
        if (person.Outcome == GuardianPersonOutcome.Forbidden) return Forbidden<GuardianLinkView>();
        if (person.Outcome == GuardianPersonOutcome.NotFound) return NotFound<GuardianLinkView>();
        if (person.Outcome != GuardianPersonOutcome.Found) return BusinessFailure<GuardianLinkView>("The guardian Person is not current.");
        return await ExecuteWithSafeRetryAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await LockPatient(command.PatientId, ct);
            var links = await db.GuardianLinks.Where(x => x.PatientProfileId == command.PatientId).ToListAsync(ct);
            if (Overlaps(links, command.GuardianPersonId, command.EffectiveFrom, command.EffectiveTo, false) || (command.IsPrimary && Overlaps(links, Guid.Empty, command.EffectiveFrom, command.EffectiveTo, true))) return Conflict<GuardianLinkView>();
            var link = GuardianLink.Create(command.PatientId, command.GuardianPersonId, command.EffectiveFrom, command.EffectiveTo, command.IsPrimary, command.Actor.ActorId, DateTimeOffset.UtcNow);
            db.GuardianLinks.Add(link); await db.SaveChangesAsync(ct);
            await CommitKnownAsync(tx, "CreateGuardianLink", ct);
            return PatientApplicationResult<GuardianLinkView>.Ok(ToView(link));
        }, command.Actor.ActorId, ct);
    }

    public async Task<PatientApplicationResult<GuardianRelationshipsResponse>> ListAsync(Guid patientId, string? kind, DateOnly? effectiveOn, int page, int pageSize, string? sort, PatientRequestActor actor, CancellationToken ct = default)
    {
        var auth = Authorize<GuardianRelationshipsResponse>(actor, PatientPermissions.ReadProfile); if (auth is not null) return auth;
        if (kind != "GUARDIAN" || page < 1 || pageSize is < 1 or > 100 || (sort is not null && sort is not ("effectiveFrom" or "-effectiveFrom"))) return Invalid<GuardianRelationshipsResponse>();
        var profile = await db.PatientProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == patientId, ct);
        if (profile is null || !actor.UnitIds.Contains(profile.PrimaryUnitId)) return NotFound<GuardianRelationshipsResponse>();
        var query = db.GuardianLinks.AsNoTracking().Where(x => x.PatientProfileId == patientId);
        if (effectiveOn is not null) query = query.Where(x => x.EffectiveFrom <= effectiveOn && (x.EffectiveTo == null || effectiveOn < x.EffectiveTo));
        var total = await query.CountAsync(ct);
        var ordered = sort == "effectiveFrom" ? query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Id) : query.OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Id);
        var links = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var persons = await people.GetManyAsync(links.Select(x => x.GuardianPersonId).Distinct().ToArray(), actor.ToPeopleActor(), ct);
        if (persons is null) return Internal<GuardianRelationshipsResponse>();
        var asOf = effectiveOn ?? Today();
        var items = links.Select(x =>
        {
            var link = ToView(x);
            return new GuardianRelationshipView(link.GuardianLinkId, link.PatientId, link.GuardianPersonId, link.EffectiveFrom, link.EffectiveTo, link.IsPrimary, link.Version, link.Etag, "GUARDIAN", State(x, asOf), new GuardianDisplayName(persons[x.GuardianPersonId].DisplayName));
        }).ToArray();
        return PatientApplicationResult<GuardianRelationshipsResponse>.Ok(new(items, page, pageSize, total));
    }

    public async Task<PatientApplicationResult<GuardianLinkView>> EndAsync(EndGuardianLinkCommand command, CancellationToken ct = default)
    {
        var auth = Authorize<GuardianLinkView>(command.Actor, ManagePermission); if (auth is not null) return auth;
        if (string.IsNullOrWhiteSpace(command.IfMatch)) return PatientApplicationResult<GuardianLinkView>.Fail(428, "PRECONDITION_REQUIRED", "If-Match is required for this operation.");
        if (!IsStrongTag(command.IfMatch) || command.PatientId == Guid.Empty || command.GuardianLinkId == Guid.Empty) return Invalid<GuardianLinkView>();
        var profile = await db.PatientProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == command.PatientId, ct);
        if (profile is null || !command.Actor.UnitIds.Contains(profile.PrimaryUnitId)) return NotFound<GuardianLinkView>();
        var patientPerson = await people.GetAsync(profile.PersonId, command.Actor.ToPeopleActor(), ct);
        if (patientPerson.Outcome == GuardianPersonOutcome.Forbidden) return Forbidden<GuardianLinkView>();
        if (patientPerson.Outcome != GuardianPersonOutcome.Found) return Internal<GuardianLinkView>();
        return await ExecuteWithSafeRetryAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await LockPatient(command.PatientId, ct);
            var link = await db.GuardianLinks.SingleOrDefaultAsync(x => x.Id == command.GuardianLinkId && x.PatientProfileId == command.PatientId, ct);
            if (link is null) return NotFound<GuardianLinkView>();
            if (!string.Equals(command.IfMatch, Etag(link), StringComparison.Ordinal)) return PatientApplicationResult<GuardianLinkView>.Fail(412, "CONCURRENCY_CONFLICT", "The GuardianLink has changed.");
            var today = Today();
            if (command.EffectiveTo < today) return PeriodFailure<GuardianLinkView>(true);
            if (!link.AppliesOn(today) || link.EffectiveTo is not null && command.EffectiveTo >= link.EffectiveTo) return StateFailure<GuardianLinkView>();
            if (command.EffectiveTo <= link.EffectiveFrom) return BusinessFailure<GuardianLinkView>("The end date must be after the start date.");
            if (profile.AdministrativeStatus == PatientAdministrativeStatus.Active && !HasMinorCoverage(await db.GuardianLinks.Where(x => x.PatientProfileId == command.PatientId && x.Id != link.Id).ToListAsync(ct), command.EffectiveTo, patientPerson.Data!.BirthDate)) return PatientApplicationResult<GuardianLinkView>.Fail(422, "GUARDIAN_COVERAGE_REQUIRED", "The end would leave a minor without legal guardian coverage.");
            link.End(command.EffectiveTo, command.Actor.ActorId, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct); await CommitKnownAsync(tx, "EndGuardianLink", ct);
            return PatientApplicationResult<GuardianLinkView>.Ok(ToView(link));
        }, command.Actor.ActorId, ct);
    }

    private async Task LockPatient(Guid patientId, CancellationToken ct) => await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM patients.patient_profile WHERE id = {patientId} FOR UPDATE", ct);
    private async Task CommitKnownAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, string operation, CancellationToken ct)
    {
        try { await tx.CommitAsync(ct); }
        catch (Exception exception)
        {
            logger.LogWarning("GuardianLink {Operation} reached an unknown commit outcome.", operation);
            throw new GuardianCommitOutcomeUnknownException(exception);
        }
    }

    private async Task<PatientApplicationResult<GuardianLinkView>> ExecuteWithSafeRetryAsync(Func<Task<PatientApplicationResult<GuardianLinkView>>> operation, Guid actorId, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxTransactionAttempts; attempt++)
        {
            try { return await operation(); }
            catch (Exception exception) when (attempt < MaxTransactionAttempts && IsProvenAbortedTransaction(exception))
            {
                db.ChangeTracker.Clear();
                logger.LogInformation("Retrying GuardianLink transaction after confirmed abort. Attempt: {Attempt}; ActorId: {ActorId}", attempt + 1, actorId);
                await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            }
            catch (GuardianCommitOutcomeUnknownException)
            {
                return Internal<GuardianLinkView>();
            }
            catch (Exception)
            {
                logger.LogWarning("GuardianLink transaction failed without a retry. ActorId: {ActorId}", actorId);
                return Internal<GuardianLinkView>();
            }
        }
        return Internal<GuardianLinkView>();
    }

    internal static bool IsProvenAbortedTransaction(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure }) return true;
        }
        return false;
    }

    private sealed class GuardianCommitOutcomeUnknownException(Exception innerException) : Exception("GuardianLink commit outcome is unknown.", innerException);
    private static bool Overlaps(IEnumerable<GuardianLink> links, Guid guardianId, DateOnly from, DateOnly? to, bool primary) => links.Any(x => (!primary ? x.GuardianPersonId == guardianId : x.IsPrimary) && x.EffectiveFrom < (to ?? DateOnly.MaxValue) && from < (x.EffectiveTo ?? DateOnly.MaxValue));
    internal static bool HasMinorCoverage(IEnumerable<GuardianLink> links, DateOnly effectiveTo, DateOnly birthDate)
    {
        var adulthood = birthDate.AddYears(18);
        if (effectiveTo >= adulthood) return true;
        var coveredUntil = effectiveTo;
        foreach (var link in links.Where(x => x.EffectiveFrom <= coveredUntil).OrderBy(x => x.EffectiveFrom))
        {
            var end = link.EffectiveTo ?? adulthood;
            if (end > coveredUntil) coveredUntil = end;
            if (coveredUntil >= adulthood) return true;
        }
        return false;
    }
    internal static string Etag(GuardianLink x) => $"\"guardian-{x.Id:N}-v{x.Version}\"";
    private static GuardianLinkView ToView(GuardianLink x) => new(x.Id, x.PatientProfileId, x.GuardianPersonId, x.EffectiveFrom, x.EffectiveTo, x.IsPrimary, x.Version, Etag(x));
    private static string State(GuardianLink x, DateOnly d) => x.EffectiveFrom > d ? "FUTURE" : x.AppliesOn(d) ? "CURRENT" : "HISTORICAL";
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    private static bool IsStrongTag(string tag) => tag.Length > 2 && tag[0] == '"' && tag[^1] == '"' && !tag.StartsWith("W/", StringComparison.Ordinal) && !tag.Contains(',') && tag != "*";
    private static PatientApplicationResult<T>? Authorize<T>(PatientRequestActor a, string permission) => !a.IsAuthenticated ? PatientApplicationResult<T>.Fail(401, "UNAUTHORIZED", "Authentication is required.") : !a.IsActive || a.HasExplicitDeny || !a.Permissions.Contains(permission) || !a.Permissions.Contains(PeoplePermissions.ReadPerson) || a.UnitIds.Count == 0 ? Forbidden<T>() : null;
    private static PatientApplicationResult<T> Invalid<T>() => PatientApplicationResult<T>.Fail(400, "VALIDATION_ERROR", "The request is invalid.");
    private static PatientApplicationResult<T> Forbidden<T>() => PatientApplicationResult<T>.Fail(403, "FORBIDDEN", "The operation is not permitted.");
    private static PatientApplicationResult<T> NotFound<T>() => PatientApplicationResult<T>.Fail(404, "RESOURCE_NOT_FOUND", "The resource was not found.");
    private static PatientApplicationResult<T> Conflict<T>() => PatientApplicationResult<T>.Fail(409, "CONFLICT", "The requested GuardianLink conflicts with an existing relationship.");
    private static PatientApplicationResult<T> StateFailure<T>() => PatientApplicationResult<T>.Fail(409, "INVALID_STATE_TRANSITION", "The GuardianLink cannot be ended in its current state.");
    private static PatientApplicationResult<T> BusinessFailure<T>(string detail) => PatientApplicationResult<T>.Fail(422, "BUSINESS_RULE_VIOLATION", detail);
    private static PatientApplicationResult<T> PeriodFailure<T>(bool retroactive) => PatientApplicationResult<T>.Fail(422, retroactive ? "RETROACTIVE_RELATIONSHIP_NOT_SUPPORTED" : "BUSINESS_RULE_VIOLATION", "The effective period is not supported.");
    private static PatientApplicationResult<T> Internal<T>() => PatientApplicationResult<T>.Fail(500, "INTERNAL_ERROR", "A referenced record is inconsistent.");
}
