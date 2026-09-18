namespace Fisiofit.Modules.Registry.Patients.Domain;

internal enum PatientAdministrativeStatus
{
    Active,
    Inactive
}

internal sealed class PatientProfile
{
    private PatientProfile()
    {
    }

    private PatientProfile(Guid id, Guid personId, Guid primaryUnitId, DateOnly relationshipStartedOn)
    {
        if (id == Guid.Empty || personId == Guid.Empty || primaryUnitId == Guid.Empty)
        {
            throw new ArgumentException("Patient, Person and Unit identifiers must be non-empty.");
        }

        Id = id;
        PersonId = personId;
        PrimaryUnitId = primaryUnitId;
        RelationshipStartedOn = relationshipStartedOn;
        AdministrativeStatus = PatientAdministrativeStatus.Active;
    }

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid PrimaryUnitId { get; private set; }
    public PatientAdministrativeStatus AdministrativeStatus { get; private set; }
    public DateOnly RelationshipStartedOn { get; private set; }

    public static PatientProfile Create(Guid personId, Guid primaryUnitId, DateOnly relationshipStartedOn) =>
        new(Guid.CreateVersion7(), personId, primaryUnitId, relationshipStartedOn);
}
