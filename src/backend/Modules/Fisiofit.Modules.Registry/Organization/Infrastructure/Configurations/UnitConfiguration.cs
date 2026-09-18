using Fisiofit.Modules.Registry.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fisiofit.Modules.Registry.Organization.Infrastructure.Configurations;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("unit", OrganizationDbContext.Schema, table =>
        {
            table.HasCheckConstraint(
                "ck_unit__status",
                "status IN ('ACTIVE', 'INACTIVE')");
        });

        builder.HasKey(unit => unit.Id).HasName("pk_unit");
        builder.Property(unit => unit.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(unit => unit.ClinicId).HasColumnName("clinic_id").IsRequired();
        builder.Property(unit => unit.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(unit => unit.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status == OrganizationStatus.Active ? "ACTIVE" : "INACTIVE",
                value => value == "ACTIVE" ? OrganizationStatus.Active : OrganizationStatus.Inactive)
            .HasMaxLength(8)
            .IsRequired();

        builder.HasOne(unit => unit.Clinic)
            .WithMany()
            .HasForeignKey(unit => unit.ClinicId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_unit__clinic");

        builder.HasIndex(unit => unit.ClinicId).HasDatabaseName("ix_unit__clinic_id");
    }
}
