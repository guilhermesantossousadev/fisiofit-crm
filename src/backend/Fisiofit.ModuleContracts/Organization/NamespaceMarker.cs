namespace Fisiofit.ModuleContracts.Organization;

public interface IValidateUnitForPatientRegistration
{
    Task<UnitValidationResult> ValidateAsync(Guid unitId, CancellationToken cancellationToken = default);
}

public enum UnitValidationOutcome
{
    Valid,
    NotFound,
    Inactive
}

public sealed record UnitValidationResult(
    UnitValidationOutcome Outcome,
    Guid UnitId,
    Guid? ClinicId = null,
    string? Name = null)
{
    public static UnitValidationResult Valid(Guid unitId, Guid clinicId, string name) =>
        new(UnitValidationOutcome.Valid, unitId, clinicId, name);

    public static UnitValidationResult NotFound(Guid unitId) =>
        new(UnitValidationOutcome.NotFound, unitId);

    public static UnitValidationResult Inactive(Guid unitId) =>
        new(UnitValidationOutcome.Inactive, unitId);
}
