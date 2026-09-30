namespace Fisiofit.Modules.Registry.Patients.Domain;

internal sealed class GuardianLink
{
    private GuardianLink() { }
    private GuardianLink(Guid patientProfileId, Guid guardianPersonId, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isPrimary, Guid actorId, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7(); PatientProfileId = patientProfileId; GuardianPersonId = guardianPersonId;
        EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo; IsPrimary = isPrimary; Version = 1;
        CreatedAt = now; CreatedByActorId = actorId;
    }
    public Guid Id { get; private set; }
    public Guid PatientProfileId { get; private set; }
    public Guid GuardianPersonId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsPrimary { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedByActorId { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public Guid? EndedByActorId { get; private set; }
    public static GuardianLink Create(Guid patientId, Guid guardianId, DateOnly from, DateOnly? to, bool primary, Guid actorId, DateTimeOffset now) => new(patientId, guardianId, from, to, primary, actorId, now);
    public void End(DateOnly effectiveTo, Guid actorId, DateTimeOffset now) { EffectiveTo = effectiveTo; EndedAt = now; EndedByActorId = actorId; Version++; }
    public bool AppliesOn(DateOnly date) => EffectiveFrom <= date && (EffectiveTo is null || date < EffectiveTo.Value);
}
