namespace Fisiofit.Modules.Registry.People.Domain;

internal enum ContactPointStatus
{
    Active,
    Inactive
}

internal sealed class ContactPoint
{
    private ContactPoint()
    {
    }

    private ContactPoint(Guid id, Guid personId, PhoneNumber phone)
    {
        Id = id;
        PersonId = personId;
        Kind = "PHONE";
        CountryCode = phone.CountryCode;
        AreaCode = phone.AreaCode;
        Number = phone.Number;
        NormalizedValue = phone.Normalized;
        IsPrimary = true;
        Status = ContactPointStatus.Active;
    }

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public string Kind { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public string AreaCode { get; private set; } = null!;
    public string Number { get; private set; } = null!;
    public string NormalizedValue { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public ContactPointStatus Status { get; private set; }
    public Person Person { get; private set; } = null!;

    public static ContactPoint CreatePrimaryPhone(Guid personId, PhoneNumber phone) =>
        new(Guid.CreateVersion7(), personId, phone);
}
