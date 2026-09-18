using Fisiofit.ModuleContracts.Organization;
using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal sealed class GetPatientDetails(
    PatientsDbContext dbContext,
    IGetPersonPatientRegistrationData personReader,
    IGetUnitForPatientRead unitReader)
{
    public async Task<PatientApplicationResult<PatientAdministrativeDetails>> ExecuteAsync(
        Guid patientId,
        PatientRequestActor actor,
        CancellationToken cancellationToken = default)
    {
        if (!actor.IsAuthenticated)
        {
            return PatientApplicationResult<PatientAdministrativeDetails>.Fail(401, "UNAUTHORIZED", "Authentication is required.");
        }

        if (!actor.IsActive
            || actor.HasExplicitDeny
            || !actor.Permissions.Contains(PatientPermissions.ReadProfile)
            || !actor.Permissions.Contains(PeoplePermissions.ReadPerson))
        {
            return PatientApplicationResult<PatientAdministrativeDetails>.Fail(403, "FORBIDDEN", "The operation is not permitted.");
        }

        var profile = await dbContext.PatientProfiles
            .AsNoTracking()
            .Where(candidate => candidate.Id == patientId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.PersonId,
                candidate.PrimaryUnitId,
                candidate.AdministrativeStatus,
                candidate.RelationshipStartedOn
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return PatientApplicationResult<PatientAdministrativeDetails>.Fail(404, "RESOURCE_NOT_FOUND", "The Patient was not found.");
        }

        if (!actor.UnitIds.Contains(profile.PrimaryUnitId))
        {
            return PatientApplicationResult<PatientAdministrativeDetails>.Fail(403, "FORBIDDEN", "The Patient is outside the actor Unit scope.");
        }

        var personResult = await personReader.GetAsync(profile.PersonId, actor.ToPeopleActor(), cancellationToken);
        if (personResult.Outcome == GetPersonPatientRegistrationDataOutcome.Forbidden)
        {
            return PatientApplicationResult<PatientAdministrativeDetails>.Fail(403, "FORBIDDEN", "The operation is not permitted.");
        }

        var unitResult = await unitReader.GetAsync(profile.PrimaryUnitId, cancellationToken);
        if (personResult.Outcome != GetPersonPatientRegistrationDataOutcome.Found || !unitResult.Found)
        {
            return PatientApplicationResult<PatientAdministrativeDetails>.Fail(500, "INTERNAL_ERROR", "A referenced record is inconsistent.");
        }

        var person = personResult.Data!;
        return PatientApplicationResult<PatientAdministrativeDetails>.Ok(
            new PatientAdministrativeDetails(
                profile.Id,
                profile.PersonId,
                person.FullName,
                person.BirthDate,
                person.PrimaryPhone,
                new PatientCpfView(person.CpfStatus, person.MaskedCpf),
                new PatientUnitView(unitResult.UnitId, unitResult.Name!, unitResult.Status!),
                profile.AdministrativeStatus == PatientAdministrativeStatus.Active ? "ACTIVE" : "INACTIVE",
                profile.RelationshipStartedOn));
    }
}
