using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.People.Domain;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fisiofit.Modules.Registry.People.Application;

internal sealed class CreatePersonForPatientRegistration(PeopleDbContext dbContext)
    : ICreatePersonForPatientRegistration
{
    private const string ReceiptConstraint = "ux_people_command_receipt__scope";
    private const string CpfConstraint = "ux_person__cpf_normalized";

    public async Task<CreatePersonForPatientRegistrationResult> ExecuteAsync(
        CreatePersonForPatientRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized(request.Actor, PeoplePermissions.CreatePerson))
        {
            return new(CreatePersonForPatientRegistrationOutcome.Forbidden);
        }

        var validation = Validate(request);
        if (validation.Errors.Count > 0)
        {
            return new(
                CreatePersonForPatientRegistrationOutcome.ValidationFailure,
                Errors: validation.Errors);
        }

        var existing = await dbContext.CommandReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt => receipt.CallerContext == "Patients"
                    && receipt.Operation == "CreatePersonForPatientRegistration"
                    && receipt.OperationKey == request.OperationKey,
                cancellationToken);

        if (existing is not null)
        {
            return existing.RequestHash == request.RequestHash
                ? new(CreatePersonForPatientRegistrationOutcome.Replayed, existing.PersonId)
                : new(CreatePersonForPatientRegistrationOutcome.OperationConflict);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var person = Person.Create(
                validation.FullName,
                request.BirthDate,
                validation.Cpf,
                validation.Phone!);
            dbContext.People.Add(person);
            dbContext.CommandReceipts.Add(PeopleCommandReceipt.Complete(
                request.OperationKey,
                request.RequestHash,
                person.Id,
                DateTimeOffset.UtcNow));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(CreatePersonForPatientRegistrationOutcome.Created, person.Id);
        }
        catch (DbUpdateException exception) when (IsConstraint(exception, ReceiptConstraint))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var racedReceipt = await dbContext.CommandReceipts
                .AsNoTracking()
                .SingleAsync(
                    receipt => receipt.CallerContext == "Patients"
                        && receipt.Operation == "CreatePersonForPatientRegistration"
                        && receipt.OperationKey == request.OperationKey,
                    cancellationToken);

            return racedReceipt.RequestHash == request.RequestHash
                ? new(CreatePersonForPatientRegistrationOutcome.Replayed, racedReceipt.PersonId)
                : new(CreatePersonForPatientRegistrationOutcome.OperationConflict);
        }
        catch (DbUpdateException exception) when (IsConstraint(exception, CpfConstraint))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return new(CreatePersonForPatientRegistrationOutcome.CpfConflict);
        }
    }

    private static bool IsAuthorized(PeopleActorContext actor, string permission) =>
        actor.IsAuthenticated
        && actor.IsActive
        && !actor.HasExplicitDeny
        && actor.Permissions.Contains(permission, StringComparer.Ordinal);

    private static ValidationResult Validate(CreatePersonForPatientRegistrationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var fullName = PeopleNormalization.NormalizeName(request.FullName);
        if (fullName.Length is < 2 or > 200)
        {
            errors["fullName"] = ["Full name must contain between 2 and 200 characters."];
        }

        if (request.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            errors["birthDate"] = ["Birth date cannot be in the future."];
        }

        var cpf = PeopleNormalization.NormalizeCpf(request.Cpf);
        if (cpf is not null && !PeopleNormalization.IsValidCpf(cpf))
        {
            errors["cpf"] = ["CPF is invalid."];
        }

        var countryCode = PeopleNormalization.Digits(request.Phone.CountryCode);
        var areaCode = PeopleNormalization.Digits(request.Phone.AreaCode);
        var number = PeopleNormalization.Digits(request.Phone.Number);
        PhoneNumber? phone = null;
        if (countryCode.Length is < 1 or > 3
            || areaCode.Length is < 1 or > 4
            || number.Length is < 6 or > 15
            || countryCode.Length + areaCode.Length + number.Length > 15)
        {
            errors["phone"] = ["Phone components are structurally invalid."];
        }
        else
        {
            phone = new PhoneNumber(countryCode, areaCode, number);
        }

        if (string.IsNullOrWhiteSpace(request.OperationKey) || request.OperationKey.Length > 200)
        {
            errors["operationKey"] = ["Operation key is invalid."];
        }

        if (request.RequestHash.Length != 64 || request.RequestHash.Any(character => !char.IsAsciiHexDigit(character)))
        {
            errors["requestHash"] = ["Request hash is invalid."];
        }

        return new(fullName, cpf, phone, errors);
    }

    private static bool IsConstraint(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: var actualConstraint
        } && string.Equals(actualConstraint, constraintName, StringComparison.Ordinal);

    private sealed record ValidationResult(
        string FullName,
        string? Cpf,
        PhoneNumber? Phone,
        IReadOnlyDictionary<string, string[]> Errors);
}
