using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Fisiofit.Modules.Registry.Patients.Infrastructure;

#nullable disable

namespace Fisiofit.Modules.Registry.Patients.Infrastructure.Migrations.Patients;

[DbContext(typeof(PatientsDbContext))]
[Migration("20260924000000_Patients_AddGuardianLinkLifecycle")]
public partial class Patients_AddGuardianLinkLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "guardian_link",
            schema: "patients",
            columns: table => new
            {
                guardian_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                guardian_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                is_primary_legal_guardian = table.Column<bool>(type: "boolean", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ended_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_guardian_link", x => x.guardian_link_id);
                table.ForeignKey(
                    name: "fk_guardian_link__patient_profile",
                    column: x => x.patient_profile_id,
                    principalSchema: "patients",
                    principalTable: "patient_profile",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_guardian_link__period", "effective_to IS NULL OR effective_to > effective_from");
                table.CheckConstraint("ck_guardian_link__version", "version > 0");
                table.CheckConstraint("ck_guardian_link__end_evidence", "(ended_at IS NULL AND ended_by_actor_id IS NULL) OR (ended_at IS NOT NULL AND ended_by_actor_id IS NOT NULL)");
            });
        migrationBuilder.CreateIndex("ux_guardian_link__patient_guardian_start", "guardian_link", new[] { "patient_profile_id", "guardian_person_id", "effective_from" }, schema: "patients", unique: true);
        migrationBuilder.CreateIndex("ix_guardian_link__patient_period", "guardian_link", new[] { "patient_profile_id", "effective_from", "effective_to" }, schema: "patients");
        migrationBuilder.CreateIndex("ix_guardian_link__guardian_person_id", "guardian_link", "guardian_person_id", schema: "patients");
        migrationBuilder.CreateIndex("ix_guardian_link__open_primary", "guardian_link", new[] { "patient_profile_id", "is_primary_legal_guardian" }, schema: "patients", filter: "is_primary_legal_guardian = TRUE AND effective_to IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "guardian_link", schema: "patients");
}
