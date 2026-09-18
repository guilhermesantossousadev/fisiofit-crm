namespace Fisiofit.Modules.Registry.People.Domain;

internal sealed class PeopleCommandReceipt
{
    private PeopleCommandReceipt()
    {
    }

    private PeopleCommandReceipt(
        Guid id,
        string operationKey,
        string requestHash,
        Guid personId,
        DateTimeOffset completedAt)
    {
        Id = id;
        CallerContext = "Patients";
        Operation = "CreatePersonForPatientRegistration";
        OperationKey = operationKey;
        RequestHash = requestHash;
        PersonId = personId;
        State = "COMPLETED";
        CreatedAt = completedAt;
        CompletedAt = completedAt;
    }

    public Guid Id { get; private set; }
    public string CallerContext { get; private set; } = null!;
    public string Operation { get; private set; } = null!;
    public string OperationKey { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public Guid PersonId { get; private set; }
    public string State { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }

    public static PeopleCommandReceipt Complete(
        string operationKey,
        string requestHash,
        Guid personId,
        DateTimeOffset completedAt) =>
        new(Guid.CreateVersion7(), operationKey, requestHash, personId, completedAt);
}
