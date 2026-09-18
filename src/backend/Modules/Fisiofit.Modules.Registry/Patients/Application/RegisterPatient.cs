using Fisiofit.ModuleContracts.Organization;
using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal sealed class RegisterPatient(
    PatientsDbContext dbContext,
    IValidateUnitForPatientRegistration unitValidator,
    ICreatePersonForPatientRegistration personCreator,
    IGetPersonPatientRegistrationData personReader)
{
    private static readonly TimeSpan ExecutionLease = TimeSpan.FromSeconds(30);
    private const string ReceiptConstraint = "ux_patient_command_receipt__scope";
    private const string PersonConstraint = "ux_patient_profile__person_id";

    public async Task<PatientApplicationResult<RegisterPatientResponse>> ExecuteAsync(
        RegisterPatientCommand command,
        CancellationToken cancellationToken = default)
    {
        var prepared = Prepare(command);
        if (prepared.Error is not null)
        {
            return prepared.Error;
        }

        var request = prepared.Request!;
        var firstUnitCheck = await unitValidator.ValidateAsync(command.PrimaryUnitId, cancellationToken);
        var unitFailure = MapUnitFailure(firstUnitCheck);
        if (unitFailure is not null)
        {
            return unitFailure;
        }

        var claim = await ClaimAsync(command.Actor.ActorId, command.IdempotencyKey!, request.RequestHash, cancellationToken);
        if (claim.Error is not null)
        {
            return claim.Error;
        }

        if (claim.Replay is not null)
        {
            return PatientApplicationResult<RegisterPatientResponse>.Ok(claim.Replay);
        }

        var receipt = claim.Receipt!;
        try
        {
            var peopleResult = await personCreator.ExecuteAsync(
                new CreatePersonForPatientRegistrationRequest(
                    request.FullName,
                    command.BirthDate,
                    request.Phone,
                    request.Cpf,
                    $"{receipt.WorkflowId:D}/person-step/v1",
                    request.PeopleHash,
                    command.Actor.ToPeopleActor(),
                    command.CorrelationId),
                cancellationToken);

            var peopleFailure = MapPeopleFailure(peopleResult);
            if (peopleFailure is not null)
            {
                await ReleaseLeaseAsync(receipt.Id, cancellationToken);
                return peopleFailure;
            }

            var personId = peopleResult.PersonId!.Value;
            await RecordPersonConfirmedAsync(receipt.Id, personId, cancellationToken);

            var personCheck = await personReader.GetAsync(personId, command.Actor.ToPeopleActor(), cancellationToken);
            if (personCheck.Outcome != GetPersonPatientRegistrationDataOutcome.Found)
            {
                await ReleaseLeaseAsync(receipt.Id, cancellationToken);
                return personCheck.Outcome == GetPersonPatientRegistrationDataOutcome.Forbidden
                    ? PatientApplicationResult<RegisterPatientResponse>.Fail(403, "FORBIDDEN", "The operation is not permitted.")
                    : PatientApplicationResult<RegisterPatientResponse>.Fail(500, "INTERNAL_ERROR", "A referenced record is inconsistent.");
            }

            var secondUnitCheck = await unitValidator.ValidateAsync(command.PrimaryUnitId, cancellationToken);
            unitFailure = MapUnitFailure(secondUnitCheck);
            if (unitFailure is not null)
            {
                await ReleaseLeaseAsync(receipt.Id, cancellationToken);
                return unitFailure;
            }

            var completed = await CompleteAsync(
                receipt.Id,
                personId,
                command.PrimaryUnitId,
                command.RelationshipStartedOn,
                cancellationToken);
            return completed;
        }
        catch
        {
            await ReleaseLeaseAsync(receipt.Id, CancellationToken.None);
            throw;
        }
    }

    private static PreparedRequest Prepare(RegisterPatientCommand command)
    {
        if (!command.Actor.IsAuthenticated)
        {
            return PreparedRequest.Fail(401, "UNAUTHORIZED", "Authentication is required.");
        }

        if (!command.Actor.IsActive
            || command.Actor.HasExplicitDeny
            || !command.Actor.Permissions.Contains(PatientPermissions.CreateProfile)
            || !command.Actor.Permissions.Contains(PeoplePermissions.CreatePerson))
        {
            return PreparedRequest.Fail(403, "FORBIDDEN", "The operation is not permitted.");
        }

        if (command.PrimaryUnitId == Guid.Empty || !command.Actor.UnitIds.Contains(command.PrimaryUnitId))
        {
            return PreparedRequest.Fail(403, "FORBIDDEN", "The requested Unit is outside the actor scope.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return PreparedRequest.Fail(400, "IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key is required.");
        }

        if (command.IdempotencyKey.Length > 200
            || command.IdempotencyKey.Any(character => character is < (char)33 or > (char)126))
        {
            return PreparedRequest.Validation("idempotencyKey", "Idempotency-Key must contain 1 to 200 visible ASCII characters.");
        }

        var fullName = PatientRequestCanonicalizer.NormalizeName(command.FullName);
        if (fullName.Length is < 2 or > 200)
        {
            return PreparedRequest.Validation("fullName", "Full name must contain between 2 and 200 characters.");
        }

        if (command.Phone is null)
        {
            return PreparedRequest.Validation("phone", "Phone is required.");
        }

        var phone = new PatientRegistrationPhone(
            PatientRequestCanonicalizer.Digits(command.Phone.CountryCode),
            PatientRequestCanonicalizer.Digits(command.Phone.AreaCode),
            PatientRequestCanonicalizer.Digits(command.Phone.Number));
        if (phone.CountryCode.Length is < 1 or > 3
            || phone.AreaCode.Length is < 1 or > 4
            || phone.Number.Length is < 6 or > 15
            || phone.CountryCode.Length + phone.AreaCode.Length + phone.Number.Length > 15)
        {
            return PreparedRequest.Validation("phone", "Phone components are structurally invalid.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (command.BirthDate == default || command.BirthDate > today || command.BirthDate > command.RelationshipStartedOn)
        {
            return PreparedRequest.Validation("birthDate", "Birth date is invalid.");
        }

        if (command.RelationshipStartedOn == default)
        {
            return PreparedRequest.Validation("relationshipStartedOn", "Relationship start date is required.");
        }

        var payerMode = PatientRequestCanonicalizer.NormalizePayerMode(command.PayerMode);
        if (payerMode != "SELF")
        {
            return PreparedRequest.Fail(422, "NON_SELF_PAYER_REQUIRES_RESPONSIBLE_PAYER_FLOW", "A different payer requires the responsible payer flow.");
        }

        if (!IsAdult(command.BirthDate, command.RelationshipStartedOn))
        {
            return PreparedRequest.Fail(422, "MINOR_REQUIRES_GUARDIAN_FLOW", "A minor requires the guardian flow.");
        }

        var cpf = PatientRequestCanonicalizer.NormalizeCpf(command.Cpf);
        if (cpf is not null && !PatientRequestCanonicalizer.IsValidCpf(cpf))
        {
            return PreparedRequest.Validation("cpf", "CPF is invalid.");
        }

        return new PreparedRequest(new CanonicalRequest(
            fullName,
            phone,
            cpf,
            PatientRequestCanonicalizer.RequestHash(
                fullName,
                command.BirthDate,
                phone,
                cpf,
                command.PrimaryUnitId,
                command.RelationshipStartedOn,
                payerMode),
            PatientRequestCanonicalizer.PeopleHash(fullName, command.BirthDate, phone, cpf)));
    }

    internal static bool IsAdult(DateOnly birthDate, DateOnly onDate)
    {
        var age = onDate.Year - birthDate.Year;
        if (birthDate.AddYears(age) > onDate)
        {
            age--;
        }

        return age >= 18;
    }

    private async Task<ClaimResult> ClaimAsync(
        Guid actorId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await dbContext.CommandReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt => receipt.ActorId == actorId
                    && receipt.Operation == "RegisterPatient"
                    && receipt.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is null)
        {
            var created = PatientCommandReceipt.Start(actorId, idempotencyKey, requestHash, now, now + ExecutionLease);
            dbContext.CommandReceipts.Add(created);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return new ClaimResult(created);
            }
            catch (DbUpdateException exception) when (IsConstraint(exception, ReceiptConstraint))
            {
                dbContext.ChangeTracker.Clear();
                return await ClaimAsync(actorId, idempotencyKey, requestHash, cancellationToken);
            }
        }

        if (existing.RequestHash != requestHash)
        {
            return ClaimResult.Fail(409, "IDEMPOTENCY_CONFLICT", "The idempotency key was used with a different request.");
        }

        if (existing.State == PatientCommandReceiptState.Completed)
        {
            return new ClaimResult(
                Replay: new RegisterPatientResponse(
                    existing.PatientId!.Value,
                    existing.PersonId!.Value,
                    existing.ResultStatus!,
                    true));
        }

        if (existing.LeaseExpiresAt > now)
        {
            return ClaimResult.Fail(409, "IDEMPOTENCY_REQUEST_IN_PROGRESS", "The request is already in progress.");
        }

        var acquired = await dbContext.CommandReceipts
            .Where(receipt => receipt.Id == existing.Id && receipt.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(receipt => receipt.LeaseExpiresAt, now + ExecutionLease)
                    .SetProperty(receipt => receipt.UpdatedAt, now),
                cancellationToken);

        return acquired == 1
            ? new ClaimResult(existing)
            : ClaimResult.Fail(409, "IDEMPOTENCY_REQUEST_IN_PROGRESS", "The request is already in progress.");
    }

    private async Task RecordPersonConfirmedAsync(Guid receiptId, Guid personId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await dbContext.CommandReceipts
            .Where(receipt => receipt.Id == receiptId && receipt.State != PatientCommandReceiptState.Completed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(receipt => receipt.PersonId, personId)
                    .SetProperty(receipt => receipt.State, PatientCommandReceiptState.PersonConfirmed)
                    .SetProperty(receipt => receipt.UpdatedAt, now),
                cancellationToken);
    }

    private async Task<PatientApplicationResult<RegisterPatientResponse>> CompleteAsync(
        Guid receiptId,
        Guid personId,
        Guid unitId,
        DateOnly relationshipStartedOn,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var receipt = await dbContext.CommandReceipts.SingleAsync(candidate => candidate.Id == receiptId, cancellationToken);
            var existingProfile = await dbContext.PatientProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(profile => profile.PersonId == personId, cancellationToken);
            if (existingProfile is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PatientApplicationResult<RegisterPatientResponse>.Fail(409, "PERSON_ALREADY_HAS_PROFILE", "The Person already has a PatientProfile.");
            }

            var profile = PatientProfile.Create(personId, unitId, relationshipStartedOn);
            dbContext.PatientProfiles.Add(profile);
            receipt.Complete(profile.Id, personId, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return PatientApplicationResult<RegisterPatientResponse>.Ok(
                new RegisterPatientResponse(profile.Id, personId, "ACTIVE", false));
        }
        catch (DbUpdateException exception) when (IsConstraint(exception, PersonConstraint))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return PatientApplicationResult<RegisterPatientResponse>.Fail(409, "PERSON_ALREADY_HAS_PROFILE", "The Person already has a PatientProfile.");
        }
    }

    private async Task ReleaseLeaseAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        _ = await dbContext.CommandReceipts
            .Where(receipt => receipt.Id == receiptId && receipt.State != PatientCommandReceiptState.Completed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(receipt => receipt.LeaseExpiresAt, now)
                    .SetProperty(receipt => receipt.UpdatedAt, now),
                cancellationToken);
    }

    private static PatientApplicationResult<RegisterPatientResponse>? MapUnitFailure(UnitValidationResult result) =>
        result.Outcome switch
        {
            UnitValidationOutcome.NotFound => PatientApplicationResult<RegisterPatientResponse>.Fail(404, "RESOURCE_NOT_FOUND", "The requested Unit was not found."),
            UnitValidationOutcome.Inactive => PatientApplicationResult<RegisterPatientResponse>.Fail(422, "UNIT_INACTIVE", "The requested Unit is inactive."),
            _ => null
        };

    private static PatientApplicationResult<RegisterPatientResponse>? MapPeopleFailure(
        CreatePersonForPatientRegistrationResult result) =>
        result.Outcome switch
        {
            CreatePersonForPatientRegistrationOutcome.CpfConflict => PatientApplicationResult<RegisterPatientResponse>.Fail(409, "CPF_ALREADY_REGISTERED", "CPF is already registered to another Person."),
            CreatePersonForPatientRegistrationOutcome.ValidationFailure => PatientApplicationResult<RegisterPatientResponse>.Fail(400, "VALIDATION_ERROR", "The request is invalid.", result.Errors),
            CreatePersonForPatientRegistrationOutcome.Forbidden => PatientApplicationResult<RegisterPatientResponse>.Fail(403, "FORBIDDEN", "The operation is not permitted."),
            CreatePersonForPatientRegistrationOutcome.OperationConflict => PatientApplicationResult<RegisterPatientResponse>.Fail(409, "IDEMPOTENCY_CONFLICT", "The workflow step identity is inconsistent."),
            CreatePersonForPatientRegistrationOutcome.DependencyFailure => PatientApplicationResult<RegisterPatientResponse>.Fail(503, "DEPENDENCY_UNAVAILABLE", "A required dependency is unavailable."),
            _ => null
        };

    private static bool IsConstraint(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: var actualConstraint
        } && string.Equals(actualConstraint, constraintName, StringComparison.Ordinal);

    private sealed record CanonicalRequest(
        string FullName,
        PatientRegistrationPhone Phone,
        string? Cpf,
        string RequestHash,
        string PeopleHash);

    private sealed record PreparedRequest(
        CanonicalRequest? Request = null,
        PatientApplicationResult<RegisterPatientResponse>? Error = null)
    {
        public static PreparedRequest Fail(int status, string code, string detail) =>
            new(Error: PatientApplicationResult<RegisterPatientResponse>.Fail(status, code, detail));

        public static PreparedRequest Validation(string field, string message) =>
            new(Error: PatientApplicationResult<RegisterPatientResponse>.Fail(
                400,
                "VALIDATION_ERROR",
                "The request is invalid.",
                new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] }));
    }

    private sealed record ClaimResult(
        PatientCommandReceipt? Receipt = null,
        RegisterPatientResponse? Replay = null,
        PatientApplicationResult<RegisterPatientResponse>? Error = null)
    {
        public static ClaimResult Fail(int status, string code, string detail) =>
            new(Error: PatientApplicationResult<RegisterPatientResponse>.Fail(status, code, detail));
    }
}
