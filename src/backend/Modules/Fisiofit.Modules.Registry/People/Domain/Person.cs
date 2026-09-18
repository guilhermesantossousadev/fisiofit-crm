namespace Fisiofit.Modules.Registry.People.Domain;

internal enum PersonRecordState
{
    Current,
    Inactive,
    MergedAlias
}

internal sealed class Person
{
    private Person()
    {
    }

    private Person(Guid id, string fullName, DateOnly birthDate, string? cpfNormalized)
    {
        Id = id;
        FullName = fullName;
        BirthDate = birthDate;
        CpfNormalized = cpfNormalized;
        RecordState = PersonRecordState.Current;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = null!;
    public DateOnly BirthDate { get; private set; }
    public string? CpfNormalized { get; private set; }
    public PersonRecordState RecordState { get; private set; }
    public IReadOnlyCollection<ContactPoint> ContactPoints => contactPoints;

    private readonly List<ContactPoint> contactPoints = [];

    public static Person Create(string fullName, DateOnly birthDate, string? cpfNormalized, PhoneNumber phone)
    {
        var person = new Person(Guid.CreateVersion7(), fullName, birthDate, cpfNormalized);
        person.contactPoints.Add(ContactPoint.CreatePrimaryPhone(person.Id, phone));
        return person;
    }
}

internal sealed record PhoneNumber(string CountryCode, string AreaCode, string Number)
{
    public string Normalized => $"+{CountryCode}{AreaCode}{Number}";
}
