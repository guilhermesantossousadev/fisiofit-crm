using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fisiofit.Modules.Registry.Patients.Infrastructure.Migrations.Patients
{
    /// <inheritdoc />
    public partial class Patients_InitialPatientRegistrationSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patients");

            migrationBuilder.CreateTable(
                name: "command_receipt",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_command_receipt", x => x.id);
                    table.CheckConstraint("ck_patient_command_receipt__state", "state IN ('STARTED', 'PERSON_CONFIRMED', 'COMPLETED')");
                });

            migrationBuilder.CreateTable(
                name: "patient_profile",
                schema: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrative_status = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    relationship_started_on = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_profile", x => x.id);
                    table.CheckConstraint("ck_patient_profile__administrative_status", "administrative_status IN ('ACTIVE', 'INACTIVE')");
                });

            migrationBuilder.CreateIndex(
                name: "ux_patient_command_receipt__scope",
                schema: "patients",
                table: "command_receipt",
                columns: new[] { "actor_id", "operation", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_patient_command_receipt__workflow_id",
                schema: "patients",
                table: "command_receipt",
                column: "workflow_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_profile__primary_unit_id",
                schema: "patients",
                table: "patient_profile",
                column: "primary_unit_id");

            migrationBuilder.CreateIndex(
                name: "ux_patient_profile__person_id",
                schema: "patients",
                table: "patient_profile",
                column: "person_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_receipt",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "patient_profile",
                schema: "patients");
        }
    }
}
