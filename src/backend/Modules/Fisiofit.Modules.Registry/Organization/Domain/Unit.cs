namespace Fisiofit.Modules.Registry.Organization.Domain;

internal sealed class Unit
{
    private Unit()
    {
    }

    private Unit(Guid id, Guid clinicId, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Unit identifier cannot be empty.", nameof(id));
        }

        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic identifier cannot be empty.", nameof(clinicId));
        }

        Id = id;
        ClinicId = clinicId;
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Unit name cannot be empty.", nameof(name))
            : name.Trim();
        Status = OrganizationStatus.Active;
    }

    public Guid Id { get; private set; }

    public Guid ClinicId { get; private set; }

    public string Name { get; private set; } = null!;

    public OrganizationStatus Status { get; private set; }

    public Clinic Clinic { get; private set; } = null!;

    public static Unit Create(Guid clinicId, string name) =>
        new(Guid.CreateVersion7(), clinicId, name);

    public static Unit Create(Guid id, Guid clinicId, string name) =>
        new(id, clinicId, name);

    public void Activate() => Status = OrganizationStatus.Active;

    public void Deactivate() => Status = OrganizationStatus.Inactive;
}
