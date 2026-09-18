using Fisiofit.ModuleContracts.Organization;
using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Organization.Application;
using Fisiofit.Modules.Registry.Patients.Application;
using Fisiofit.Modules.Registry.People.Application;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fisiofit.IntegrationTests.Registry.PatientRegistration;

[Collection(PatientRegistrationPostgreSqlCollection.Name)]
public sealed class RegisterPatientWorkflowTests(PatientRegistrationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task SuccessfulWorkflow_PersistsBothOwnersAndReplaysCompletedResult()
    {
        var command = Command($"workflow-success-{Guid.CreateVersion7():N}");

        var first = await ExecuteAsync(command);
        var replay = await ExecuteAsync(command);

        Assert.True(first.Success);
        Assert.True(replay.Success);
        Assert.False(first.Value!.Replayed);
        Assert.True(replay.Value!.Replayed);
        Assert.Equal(first.Value.PatientId, replay.Value.PatientId);
        Assert.Equal(first.Value.PersonId, replay.Value.PersonId);

        await using var people = fixture.CreatePeopleDbContext();
        await using var patients = fixture.CreatePatientsDbContext();
        Assert.Equal(1, await people.People.CountAsync(person => person.Id == first.Value.PersonId));
        Assert.Equal(1, await patients.PatientProfiles.CountAsync(profile => profile.Id == first.Value.PatientId));
    }

    [Fact]
    public async Task SameKeyWithDifferentSemanticRequest_ReturnsIdempotencyConflict()
    {
        var key = $"workflow-mismatch-{Guid.CreateVersion7():N}";
        var first = await ExecuteAsync(Command(key));
        var changed = Command(key) with { FullName = "Outra Pessoa Fictícia" };

        var mismatch = await ExecuteAsync(changed);

        Assert.True(first.Success);
        Assert.False(mismatch.Success);
        Assert.Equal(409, mismatch.StatusCode);
        Assert.Equal("IDEMPOTENCY_CONFLICT", mismatch.Code);
    }

    [Fact]
    public async Task UnitBecomesInactiveAfterPeopleCommit_RetryReusesPersonAndCompletesProfile()
    {
        var command = Command($"workflow-recovery-{Guid.CreateVersion7():N}");
        await using var peopleDb = fixture.CreatePeopleDbContext();
        await using var patientsDb = fixture.CreatePatientsDbContext();
        var peopleCreate = new CreatePersonForPatientRegistration(peopleDb);
        var peopleRead = new GetPersonPatientRegistrationData(peopleDb);
        var handler = new RegisterPatient(
            patientsDb,
            new SequenceUnitValidator(fixture),
            peopleCreate,
            peopleRead);

        var interrupted = await handler.ExecuteAsync(command);

        Assert.False(interrupted.Success);
        Assert.Equal("UNIT_INACTIVE", interrupted.Code);
        var receipt = await patientsDb.CommandReceipts
            .AsNoTracking()
            .SingleAsync(candidate => candidate.IdempotencyKey == command.IdempotencyKey);
        Assert.NotNull(receipt.PersonId);
        Assert.Equal(0, await patientsDb.PatientProfiles.CountAsync(profile => profile.PersonId == receipt.PersonId));
        Assert.Equal(1, await peopleDb.People.CountAsync(person => person.Id == receipt.PersonId));

        var resumed = await ExecuteAsync(command);

        Assert.True(resumed.Success);
        Assert.Equal(receipt.PersonId, resumed.Value!.PersonId);
        await using var verifyPeople = fixture.CreatePeopleDbContext();
        Assert.Equal(1, await verifyPeople.People.CountAsync(person => person.Id == receipt.PersonId));
    }

    [Fact]
    public async Task PeopleCommitWithLostResponse_RetryUsesStableOperationKey()
    {
        var command = Command($"workflow-lost-people-response-{Guid.CreateVersion7():N}");
        await using var organization = fixture.CreateOrganizationDbContext();
        await using var people = fixture.CreatePeopleDbContext();
        await using var patients = fixture.CreatePatientsDbContext();
        var losingCreator = new LoseCommittedPeopleResponse(new CreatePersonForPatientRegistration(people));
        var interruptedHandler = new RegisterPatient(
            patients,
            new ValidateUnitForPatientRegistration(organization),
            losingCreator,
            new GetPersonPatientRegistrationData(people));

        await Assert.ThrowsAsync<InvalidOperationException>(() => interruptedHandler.ExecuteAsync(command));

        var partialReceipt = await patients.CommandReceipts
            .AsNoTracking()
            .SingleAsync(candidate => candidate.IdempotencyKey == command.IdempotencyKey);
        Assert.Null(partialReceipt.PersonId);
        var peopleReceipt = await people.CommandReceipts.AsNoTracking().SingleAsync(
            candidate => candidate.OperationKey == $"{partialReceipt.WorkflowId:D}/person-step/v1");

        var resumed = await ExecuteAsync(command);

        Assert.True(resumed.Success);
        Assert.Equal(peopleReceipt.PersonId, resumed.Value!.PersonId);
        await using var verifyPeople = fixture.CreatePeopleDbContext();
        Assert.Equal(1, await verifyPeople.People.CountAsync(candidate => candidate.Id == peopleReceipt.PersonId));
    }

    [Fact]
    public async Task MinorAndNonSelfPayer_FailBeforePeopleWrite()
    {
        await using var beforeDb = fixture.CreatePeopleDbContext();
        var before = await beforeDb.People.CountAsync();

        var minor = await ExecuteAsync(Command($"minor-{Guid.CreateVersion7():N}") with
        {
            BirthDate = new DateOnly(2015, 1, 1)
        });
        var otherPayer = await ExecuteAsync(Command($"payer-{Guid.CreateVersion7():N}") with
        {
            PayerMode = "OTHER"
        });

        await using var afterDb = fixture.CreatePeopleDbContext();
        Assert.Equal("MINOR_REQUIRES_GUARDIAN_FLOW", minor.Code);
        Assert.Equal("NON_SELF_PAYER_REQUIRES_RESPONSIBLE_PAYER_FLOW", otherPayer.Code);
        Assert.Equal(before, await afterDb.People.CountAsync());
    }

    [Fact]
    public async Task ConcurrentSameIdempotencyKey_HasOneLogicalExecution()
    {
        var command = Command($"workflow-concurrent-{Guid.CreateVersion7():N}");
        await using var organizationOne = fixture.CreateOrganizationDbContext();
        await using var peopleOne = fixture.CreatePeopleDbContext();
        await using var patientsOne = fixture.CreatePatientsDbContext();
        await using var organizationTwo = fixture.CreateOrganizationDbContext();
        await using var peopleTwo = fixture.CreatePeopleDbContext();
        await using var patientsTwo = fixture.CreatePatientsDbContext();
        var blockingCreator = new BlockingPersonCreator(new CreatePersonForPatientRegistration(peopleOne));
        var firstHandler = new RegisterPatient(
            patientsOne,
            new ValidateUnitForPatientRegistration(organizationOne),
            blockingCreator,
            new GetPersonPatientRegistrationData(peopleOne));
        var secondHandler = new RegisterPatient(
            patientsTwo,
            new ValidateUnitForPatientRegistration(organizationTwo),
            new CreatePersonForPatientRegistration(peopleTwo),
            new GetPersonPatientRegistrationData(peopleTwo));

        var firstTask = firstHandler.ExecuteAsync(command);
        await blockingCreator.Entered;
        var concurrent = await secondHandler.ExecuteAsync(command);
        blockingCreator.Release();
        var first = await firstTask;

        Assert.True(first.Success);
        Assert.False(concurrent.Success);
        Assert.Equal("IDEMPOTENCY_REQUEST_IN_PROGRESS", concurrent.Code);
        await using var verifyPeople = fixture.CreatePeopleDbContext();
        await using var verifyPatients = fixture.CreatePatientsDbContext();
        Assert.Equal(1, await verifyPeople.CommandReceipts.CountAsync(receipt => receipt.PersonId == first.Value!.PersonId));
        Assert.Equal(1, await verifyPatients.CommandReceipts.CountAsync(receipt => receipt.IdempotencyKey == command.IdempotencyKey));
        Assert.Equal(1, await verifyPatients.PatientProfiles.CountAsync(profile => profile.Id == first.Value!.PatientId));
    }

    [Fact]
    public async Task ConcurrentDifferentRequestsWithSameCpf_CreateAtMostOnePersonAndOneProfile()
    {
        const string fictitiousCpf = "123.456.789-09";
        var firstCommand = Command($"cpf-race-one-{Guid.CreateVersion7():N}") with { Cpf = fictitiousCpf };
        var secondCommand = Command($"cpf-race-two-{Guid.CreateVersion7():N}") with
        {
            FullName = "Outra Pessoa Fictícia da Corrida",
            Cpf = fictitiousCpf
        };

        var results = await Task.WhenAll(ExecuteAsync(firstCommand), ExecuteAsync(secondCommand));

        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => result.Code == "CPF_ALREADY_REGISTERED");
        await using var people = fixture.CreatePeopleDbContext();
        await using var patients = fixture.CreatePatientsDbContext();
        Assert.Equal(1, await people.People.CountAsync(person => person.CpfNormalized == "12345678909"));
        var winnerPersonId = results.Single(result => result.Success).Value!.PersonId;
        Assert.Equal(1, await patients.PatientProfiles.CountAsync(profile => profile.PersonId == winnerPersonId));
    }

    [Fact]
    public async Task FailureBeforePeopleCommit_CreatesNoPerson()
    {
        var command = Command($"people-failure-{Guid.CreateVersion7():N}");
        await using var organization = fixture.CreateOrganizationDbContext();
        await using var people = fixture.CreatePeopleDbContext();
        await using var patients = fixture.CreatePatientsDbContext();
        var before = await people.People.CountAsync();
        var profilesBefore = await patients.PatientProfiles.CountAsync();
        var handler = new RegisterPatient(
            patients,
            new ValidateUnitForPatientRegistration(organization),
            new ThrowingPersonCreator(),
            new GetPersonPatientRegistrationData(people));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(command));

        people.ChangeTracker.Clear();
        Assert.Equal(before, await people.People.CountAsync());
        Assert.Equal(profilesBefore, await patients.PatientProfiles.CountAsync());
        var receipt = await patients.CommandReceipts.AsNoTracking().SingleAsync(candidate => candidate.IdempotencyKey == command.IdempotencyKey);
        Assert.Null(receipt.PersonId);
    }

    private async Task<PatientApplicationResult<RegisterPatientResponse>> ExecuteAsync(RegisterPatientCommand command)
    {
        await using var organization = fixture.CreateOrganizationDbContext();
        await using var people = fixture.CreatePeopleDbContext();
        await using var patients = fixture.CreatePatientsDbContext();
        var handler = new RegisterPatient(
            patients,
            new ValidateUnitForPatientRegistration(organization),
            new CreatePersonForPatientRegistration(people),
            new GetPersonPatientRegistrationData(people));
        return await handler.ExecuteAsync(command);
    }

    private RegisterPatientCommand Command(string key) => new(
        "Pessoa Fictícia do Workflow",
        new DateOnly(1990, 1, 1),
        new PatientRegistrationPhone("999", "000", "000000"),
        null,
        fixture.ActiveUnitId,
        new DateOnly(2026, 9, 18),
        "SELF",
        key,
        Actor(),
        "integration-test-correlation");

    private PatientRequestActor Actor() => new(
        Guid.Parse("0199ffee-0000-7000-8000-000000000201"),
        true,
        true,
        false,
        new HashSet<string>(StringComparer.Ordinal)
        {
            PatientPermissions.CreateProfile,
            PatientPermissions.ReadProfile,
            PeoplePermissions.CreatePerson,
            PeoplePermissions.ReadPerson
        },
        new HashSet<Guid> { fixture.ActiveUnitId, fixture.InactiveUnitId });

    private sealed class SequenceUnitValidator(PatientRegistrationPostgreSqlFixture fixture)
        : IValidateUnitForPatientRegistration
    {
        private int calls;

        public Task<UnitValidationResult> ValidateAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            calls++;
            return Task.FromResult(calls == 1
                ? UnitValidationResult.Valid(unitId, fixture.ClinicId, "Unidade Fictícia Ativa")
                : UnitValidationResult.Inactive(unitId));
        }
    }

    private sealed class BlockingPersonCreator(ICreatePersonForPatientRegistration inner)
        : ICreatePersonForPatientRegistration
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => entered.Task;

        public void Release() => released.TrySetResult();

        public async Task<CreatePersonForPatientRegistrationResult> ExecuteAsync(
            CreatePersonForPatientRegistrationRequest request,
            CancellationToken cancellationToken = default)
        {
            entered.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return await inner.ExecuteAsync(request, cancellationToken);
        }
    }

    private sealed class ThrowingPersonCreator : ICreatePersonForPatientRegistration
    {
        public Task<CreatePersonForPatientRegistrationResult> ExecuteAsync(
            CreatePersonForPatientRegistrationRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Controlled test failure before People commit.");
    }

    private sealed class LoseCommittedPeopleResponse(ICreatePersonForPatientRegistration inner)
        : ICreatePersonForPatientRegistration
    {
        public async Task<CreatePersonForPatientRegistrationResult> ExecuteAsync(
            CreatePersonForPatientRegistrationRequest request,
            CancellationToken cancellationToken = default)
        {
            _ = await inner.ExecuteAsync(request, cancellationToken);
            throw new InvalidOperationException("Controlled loss after People commit.");
        }
    }
}
