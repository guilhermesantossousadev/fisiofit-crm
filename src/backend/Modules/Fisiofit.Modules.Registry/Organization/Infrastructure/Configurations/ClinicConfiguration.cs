using Fisiofit.Modules.Registry.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fisiofit.Modules.Registry.Organization.Infrastructure.Configurations;

internal sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("clinic", OrganizationDbContext.Schema, table =>
        {
            table.HasCheckConstraint(
                "ck_clinic__status",
                "status IN ('ACTIVE', 'INACTIVE')");
        });

        builder.HasKey(clinic => clinic.Id).HasName("pk_clinic");
        builder.Property(clinic => clinic.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(clinic => clinic.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(clinic => clinic.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status == OrganizationStatus.Active ? "ACTIVE" : "INACTIVE",
                value => value == "ACTIVE" ? OrganizationStatus.Active : OrganizationStatus.Inactive)
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(clinic => clinic.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .IsRequired();
    }
}
