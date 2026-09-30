using Fisiofit.Modules.Registry.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fisiofit.Modules.Registry.Patients.Infrastructure.Configurations;

internal sealed class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.ToTable("patient_profile", PatientsDbContext.Schema, table =>
            table.HasCheckConstraint("ck_patient_profile__administrative_status", "administrative_status IN ('ACTIVE', 'INACTIVE')"));
        builder.HasKey(profile => profile.Id).HasName("pk_patient_profile");
        builder.Property(profile => profile.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(profile => profile.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(profile => profile.PrimaryUnitId).HasColumnName("primary_unit_id").IsRequired();
        builder.Property(profile => profile.RelationshipStartedOn).HasColumnName("relationship_started_on").IsRequired();
        builder.Property(profile => profile.AdministrativeStatus)
            .HasColumnName("administrative_status")
            .HasConversion(value => value == PatientAdministrativeStatus.Active ? "ACTIVE" : "INACTIVE", value => value == "ACTIVE" ? PatientAdministrativeStatus.Active : PatientAdministrativeStatus.Inactive)
            .HasMaxLength(8)
            .IsRequired();
        builder.HasIndex(profile => profile.PersonId)
            .IsUnique()
            .HasDatabaseName("ux_patient_profile__person_id");
        builder.HasIndex(profile => profile.PrimaryUnitId)
            .HasDatabaseName("ix_patient_profile__primary_unit_id");
    }
}

internal sealed class PatientCommandReceiptConfiguration : IEntityTypeConfiguration<PatientCommandReceipt>
{
    public void Configure(EntityTypeBuilder<PatientCommandReceipt> builder)
    {
        builder.ToTable("command_receipt", PatientsDbContext.Schema, table =>
            table.HasCheckConstraint("ck_patient_command_receipt__state", "state IN ('STARTED', 'PERSON_CONFIRMED', 'COMPLETED')"));
        builder.HasKey(receipt => receipt.Id).HasName("pk_command_receipt");
        builder.Property(receipt => receipt.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(receipt => receipt.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(receipt => receipt.Operation).HasColumnName("operation").HasMaxLength(100).IsRequired();
        builder.Property(receipt => receipt.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        builder.Property(receipt => receipt.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
        builder.Property(receipt => receipt.WorkflowId).HasColumnName("workflow_id").IsRequired();
        builder.Property(receipt => receipt.State)
            .HasColumnName("state")
            .HasConversion(
                value => value == PatientCommandReceiptState.Started ? "STARTED" : value == PatientCommandReceiptState.PersonConfirmed ? "PERSON_CONFIRMED" : "COMPLETED",
                value => value == "STARTED" ? PatientCommandReceiptState.Started : value == "PERSON_CONFIRMED" ? PatientCommandReceiptState.PersonConfirmed : PatientCommandReceiptState.Completed)
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(receipt => receipt.PersonId).HasColumnName("person_id");
        builder.Property(receipt => receipt.PatientId).HasColumnName("patient_id");
        builder.Property(receipt => receipt.ResultStatus).HasColumnName("result_status").HasMaxLength(20);
        builder.Property(receipt => receipt.LeaseExpiresAt).HasColumnName("lease_expires_at").IsRequired();
        builder.Property(receipt => receipt.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(receipt => receipt.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(receipt => receipt.CompletedAt).HasColumnName("completed_at");
        builder.HasIndex(receipt => new { receipt.ActorId, receipt.Operation, receipt.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_patient_command_receipt__scope");
        builder.HasIndex(receipt => receipt.WorkflowId)
            .IsUnique()
            .HasDatabaseName("ux_patient_command_receipt__workflow_id");
    }
}

internal sealed class GuardianLinkConfiguration : IEntityTypeConfiguration<GuardianLink>
{
    public void Configure(EntityTypeBuilder<GuardianLink> builder)
    {
        builder.ToTable("guardian_link", PatientsDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_guardian_link__period", "effective_to IS NULL OR effective_to > effective_from");
            table.HasCheckConstraint("ck_guardian_link__version", "version > 0");
            table.HasCheckConstraint("ck_guardian_link__end_evidence", "(ended_at IS NULL AND ended_by_actor_id IS NULL) OR (ended_at IS NOT NULL AND ended_by_actor_id IS NOT NULL)");
        });
        builder.HasKey(x => x.Id).HasName("pk_guardian_link");
        builder.Property(x => x.Id).HasColumnName("guardian_link_id").ValueGeneratedNever();
        builder.Property(x => x.PatientProfileId).HasColumnName("patient_profile_id").IsRequired();
        builder.Property(x => x.GuardianPersonId).HasColumnName("guardian_person_id").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to");
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary_legal_guardian").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedByActorId).HasColumnName("created_by_actor_id").IsRequired();
        builder.Property(x => x.EndedAt).HasColumnName("ended_at");
        builder.Property(x => x.EndedByActorId).HasColumnName("ended_by_actor_id");
        builder.HasOne<PatientProfile>().WithMany().HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_guardian_link__patient_profile");
        builder.HasIndex(x => new { x.PatientProfileId, x.GuardianPersonId, x.EffectiveFrom }).IsUnique().HasDatabaseName("ux_guardian_link__patient_guardian_start");
        builder.HasIndex(x => new { x.PatientProfileId, x.EffectiveFrom, x.EffectiveTo }).HasDatabaseName("ix_guardian_link__patient_period");
        builder.HasIndex(x => x.GuardianPersonId).HasDatabaseName("ix_guardian_link__guardian_person_id");
        builder.HasIndex(x => new { x.PatientProfileId, x.IsPrimary }).HasFilter("is_primary_legal_guardian = TRUE AND effective_to IS NULL").HasDatabaseName("ix_guardian_link__open_primary");
    }
}
