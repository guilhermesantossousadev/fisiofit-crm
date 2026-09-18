namespace Fisiofit.Modules.Registry.Organization.Domain;

internal sealed class Clinic
{
    private Clinic()
    {
    }

    private Clinic(Guid id, string name, string timeZoneId)
    {
        Id = id;
        Name = RequireText(name, nameof(name));
        TimeZoneId = RequireTimeZone(timeZoneId);
        Status = OrganizationStatus.Active;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public OrganizationStatus Status { get; private set; }

    public string TimeZoneId { get; private set; } = null!;

    public static Clinic Create(string name, string timeZoneId) =>
        new(Guid.CreateVersion7(), name, timeZoneId);

    public static Clinic Create(Guid id, string name, string timeZoneId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Clinic identifier cannot be empty.", nameof(id));
        }

        return new Clinic(id, name, timeZoneId);
    }

    public void Activate() => Status = OrganizationStatus.Active;

    public void Deactivate() => Status = OrganizationStatus.Inactive;

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string RequireTimeZone(string timeZoneId)
    {
        var normalized = RequireText(timeZoneId, nameof(timeZoneId));

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalized);
            return normalized;
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException("Time zone must be a valid IANA identifier.", nameof(timeZoneId), exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException("Time zone must be a valid IANA identifier.", nameof(timeZoneId), exception);
        }
    }
}
