using Fisiofit.Modules.Registry.People.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fisiofit.Modules.Registry.People.Infrastructure.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("person", PeopleDbContext.Schema, table =>
            table.HasCheckConstraint("ck_person__record_state", "record_state IN ('CURRENT', 'INACTIVE', 'MERGED_ALIAS')"));
        builder.HasKey(person => person.Id).HasName("pk_person");
        builder.Property(person => person.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(person => person.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(person => person.BirthDate).HasColumnName("birth_date").IsRequired();
        builder.Property(person => person.CpfNormalized).HasColumnName("cpf_normalized").HasMaxLength(11);
        builder.Property(person => person.RecordState)
            .HasColumnName("record_state")
            .HasConversion(
                value => value == PersonRecordState.Current ? "CURRENT" : value == PersonRecordState.Inactive ? "INACTIVE" : "MERGED_ALIAS",
                value => value == "CURRENT" ? PersonRecordState.Current : value == "INACTIVE" ? PersonRecordState.Inactive : PersonRecordState.MergedAlias)
            .HasMaxLength(12)
            .IsRequired();
        builder.HasIndex(person => person.CpfNormalized)
            .IsUnique()
            .HasFilter("cpf_normalized IS NOT NULL")
            .HasDatabaseName("ux_person__cpf_normalized");
        builder.HasMany(person => person.ContactPoints)
            .WithOne(contact => contact.Person)
            .HasForeignKey(contact => contact.PersonId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_contact_point__person");
        builder.Navigation(person => person.ContactPoints).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ContactPointConfiguration : IEntityTypeConfiguration<ContactPoint>
{
    public void Configure(EntityTypeBuilder<ContactPoint> builder)
    {
        builder.ToTable("contact_point", PeopleDbContext.Schema, table =>
        {
            table.HasCheckConstraint("ck_contact_point__kind", "kind = 'PHONE'");
            table.HasCheckConstraint("ck_contact_point__status", "status IN ('ACTIVE', 'INACTIVE')");
        });
        builder.HasKey(contact => contact.Id).HasName("pk_contact_point");
        builder.Property(contact => contact.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(contact => contact.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(contact => contact.Kind).HasColumnName("kind").HasMaxLength(10).IsRequired();
        builder.Property(contact => contact.CountryCode).HasColumnName("country_code").HasMaxLength(3).IsRequired();
        builder.Property(contact => contact.AreaCode).HasColumnName("area_code").HasMaxLength(4).IsRequired();
        builder.Property(contact => contact.Number).HasColumnName("number").HasMaxLength(15).IsRequired();
        builder.Property(contact => contact.NormalizedValue).HasColumnName("normalized_value").HasMaxLength(16).IsRequired();
        builder.Property(contact => contact.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(contact => contact.Status)
            .HasColumnName("status")
            .HasConversion(value => value == ContactPointStatus.Active ? "ACTIVE" : "INACTIVE", value => value == "ACTIVE" ? ContactPointStatus.Active : ContactPointStatus.Inactive)
            .HasMaxLength(8)
            .IsRequired();
        builder.HasIndex(contact => contact.PersonId).HasDatabaseName("ix_contact_point__person_id");
        builder.HasIndex(contact => contact.PersonId)
            .IsUnique()
            .HasFilter("kind = 'PHONE' AND is_primary = TRUE AND status = 'ACTIVE'")
            .HasDatabaseName("ux_contact_point__active_primary_phone");
    }
}

internal sealed class PeopleCommandReceiptConfiguration : IEntityTypeConfiguration<PeopleCommandReceipt>
{
    public void Configure(EntityTypeBuilder<PeopleCommandReceipt> builder)
    {
        builder.ToTable("command_receipt", PeopleDbContext.Schema, table =>
            table.HasCheckConstraint("ck_people_command_receipt__state", "state = 'COMPLETED'"));
        builder.HasKey(receipt => receipt.Id).HasName("pk_command_receipt");
        builder.Property(receipt => receipt.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(receipt => receipt.CallerContext).HasColumnName("caller_context").HasMaxLength(50).IsRequired();
        builder.Property(receipt => receipt.Operation).HasColumnName("operation").HasMaxLength(100).IsRequired();
        builder.Property(receipt => receipt.OperationKey).HasColumnName("operation_key").HasMaxLength(200).IsRequired();
        builder.Property(receipt => receipt.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
        builder.Property(receipt => receipt.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(receipt => receipt.State).HasColumnName("state").HasMaxLength(20).IsRequired();
        builder.Property(receipt => receipt.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(receipt => receipt.CompletedAt).HasColumnName("completed_at").IsRequired();
        builder.HasIndex(receipt => new { receipt.CallerContext, receipt.Operation, receipt.OperationKey })
            .IsUnique()
            .HasDatabaseName("ux_people_command_receipt__scope");
    }
}
