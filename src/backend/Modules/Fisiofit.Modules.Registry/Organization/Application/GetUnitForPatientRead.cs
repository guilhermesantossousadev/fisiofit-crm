using Fisiofit.ModuleContracts.Organization;
using Fisiofit.Modules.Registry.Organization.Domain;
using Fisiofit.Modules.Registry.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fisiofit.Modules.Registry.Organization.Application;

internal sealed class GetUnitForPatientRead(OrganizationDbContext dbContext) : IGetUnitForPatientRead
{
    public async Task<UnitPatientReadResult> GetAsync(
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units
            .AsNoTracking()
            .Where(candidate => candidate.Id == unitId)
            .Select(candidate => new { candidate.Id, candidate.Name, candidate.Status })
            .SingleOrDefaultAsync(cancellationToken);

        return unit is null
            ? UnitPatientReadResult.Missing(unitId)
            : UnitPatientReadResult.Existing(
                unit.Id,
                unit.Name,
                unit.Status == OrganizationStatus.Active ? "ACTIVE" : "INACTIVE");
    }
}
