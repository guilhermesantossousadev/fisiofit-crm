using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.People.Domain;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.People.Application;

internal sealed class GetPersonPatientRegistrationData(PeopleDbContext dbContext)
    : IGetPersonPatientRegistrationData
{
    public async Task<GetPersonPatientRegistrationDataResult> GetAsync(
        Guid personId,
        PeopleActorContext actor,
        CancellationToken cancellationToken = default)
    {
        if (!actor.IsAuthenticated
            || !actor.IsActive
            || actor.HasExplicitDeny
            || (!actor.Permissions.Contains(PeoplePermissions.ReadPerson, StringComparer.Ordinal)
                && !actor.Permissions.Contains(PeoplePermissions.CreatePerson, StringComparer.Ordinal)))
        {
            return new(GetPersonPatientRegistrationDataOutcome.Forbidden);
        }

        var person = await dbContext.People
            .AsNoTracking()
            .Where(candidate => candidate.Id == personId && candidate.RecordState == PersonRecordState.Current)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.FullName,
                candidate.BirthDate,
                candidate.CpfNormalized,
                Phone = candidate.ContactPoints
                    .Where(contact => contact.Kind == "PHONE" && contact.IsPrimary && contact.Status == ContactPointStatus.Active)
                    .Select(contact => new { contact.CountryCode, contact.AreaCode, contact.Number })
                    .SingleOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (person?.Phone is null)
        {
            return new(GetPersonPatientRegistrationDataOutcome.NotFound);
        }

        var cpfStatus = person.CpfNormalized is null ? "ABSENT" : "PRESENT";
        var maskedCpf = person.CpfNormalized is null
            ? null
            : $"***.***.***-{person.CpfNormalized[^2..]}";

        return new(
            GetPersonPatientRegistrationDataOutcome.Found,
            new PersonPatientRegistrationData(
                person.Id,
                person.FullName,
                person.BirthDate,
                new PatientRegistrationPhone(
                    person.Phone.CountryCode,
                    person.Phone.AreaCode,
                    person.Phone.Number),
                cpfStatus,
                maskedCpf));
    }
}
