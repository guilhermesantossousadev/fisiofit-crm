using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fisiofit.Modules.Registry.People.Infrastructure.Migrations.People
{
    /// <inheritdoc />
    public partial class People_InitialPatientRegistrationSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "people");

            migrationBuilder.CreateTable(
                name: "command_receipt",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caller_context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operation_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_command_receipt", x => x.id);
                    table.CheckConstraint("ck_people_command_receipt__state", "state = 'COMPLETED'");
                });

            migrationBuilder.CreateTable(
                name: "person",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cpf_normalized = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    record_state = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person", x => x.id);
                    table.CheckConstraint("ck_person__record_state", "record_state IN ('CURRENT', 'INACTIVE', 'MERGED_ALIAS')");
                });

            migrationBuilder.CreateTable(
                name: "contact_point",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    area_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_point", x => x.id);
                    table.CheckConstraint("ck_contact_point__kind", "kind = 'PHONE'");
                    table.CheckConstraint("ck_contact_point__status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.ForeignKey(
                        name: "fk_contact_point__person",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_people_command_receipt__scope",
                schema: "people",
                table: "command_receipt",
                columns: new[] { "caller_context", "operation", "operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_contact_point__active_primary_phone",
                schema: "people",
                table: "contact_point",
                column: "person_id",
                unique: true,
                filter: "kind = 'PHONE' AND is_primary = TRUE AND status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "ux_person__cpf_normalized",
                schema: "people",
                table: "person",
                column: "cpf_normalized",
                unique: true,
                filter: "cpf_normalized IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_receipt",
                schema: "people");

            migrationBuilder.DropTable(
                name: "contact_point",
                schema: "people");

            migrationBuilder.DropTable(
                name: "person",
                schema: "people");
        }
    }
}
