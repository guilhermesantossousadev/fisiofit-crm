using Fisiofit.ModuleContracts.Organization;
using Fisiofit.Modules.Registry.Organization.Domain;
using Fisiofit.Modules.Registry.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.Organization.Application;

internal sealed class ValidateUnitForPatientRegistration(OrganizationDbContext dbContext)
    : IValidateUnitForPatientRegistration
{
    public async Task<UnitValidationResult> ValidateAsync(
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units
            .AsNoTracking()
            .Where(candidate => candidate.Id == unitId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.ClinicId,
                candidate.Name,
                candidate.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (unit is null)
        {
            return UnitValidationResult.NotFound(unitId);
        }

        return unit.Status == OrganizationStatus.Active
            ? UnitValidationResult.Valid(unit.Id, unit.ClinicId, unit.Name)
            : UnitValidationResult.Inactive(unit.Id);
    }
}
