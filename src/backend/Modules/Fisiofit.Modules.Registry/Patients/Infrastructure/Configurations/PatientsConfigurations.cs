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
