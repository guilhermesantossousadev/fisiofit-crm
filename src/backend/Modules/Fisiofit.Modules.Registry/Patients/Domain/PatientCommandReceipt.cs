namespace Fisiofit.Modules.Registry.Patients.Domain;

internal enum PatientCommandReceiptState
{
    Started,
    PersonConfirmed,
    Completed
}

internal sealed class PatientCommandReceipt
{
    private PatientCommandReceipt()
    {
    }

    private PatientCommandReceipt(
        Guid id,
        Guid actorId,
        string idempotencyKey,
        string requestHash,
        DateTimeOffset now,
        DateTimeOffset leaseExpiresAt)
    {
        Id = id;
        ActorId = actorId;
        Operation = "RegisterPatient";
        IdempotencyKey = idempotencyKey;
        RequestHash = requestHash;
        WorkflowId = Guid.CreateVersion7();
        State = PatientCommandReceiptState.Started;
        CreatedAt = now;
        UpdatedAt = now;
        LeaseExpiresAt = leaseExpiresAt;
    }

    public Guid Id { get; private set; }
    public Guid ActorId { get; private set; }
    public string Operation { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public Guid WorkflowId { get; private set; }
    public PatientCommandReceiptState State { get; private set; }
    public Guid? PersonId { get; private set; }
    public Guid? PatientId { get; private set; }
    public string? ResultStatus { get; private set; }
    public DateTimeOffset LeaseExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static PatientCommandReceipt Start(
        Guid actorId,
        string idempotencyKey,
        string requestHash,
        DateTimeOffset now,
        DateTimeOffset leaseExpiresAt) =>
        new(Guid.CreateVersion7(), actorId, idempotencyKey, requestHash, now, leaseExpiresAt);

    public void ConfirmPerson(Guid personId, DateTimeOffset now)
    {
        PersonId = personId;
        State = PatientCommandReceiptState.PersonConfirmed;
        UpdatedAt = now;
    }

    public void Complete(Guid patientId, Guid personId, DateTimeOffset now)
    {
        PatientId = patientId;
        PersonId = personId;
        ResultStatus = "ACTIVE";
        State = PatientCommandReceiptState.Completed;
        UpdatedAt = now;
        CompletedAt = now;
        LeaseExpiresAt = now;
    }
}
