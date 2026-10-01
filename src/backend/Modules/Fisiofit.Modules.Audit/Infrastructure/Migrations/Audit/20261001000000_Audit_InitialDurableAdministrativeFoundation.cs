using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fisiofit.Modules.Audit.Infrastructure.Migrations.Audit;

[DbContext(typeof(AuditDbContext))]
[Migration("20261001000000_Audit_InitialDurableAdministrativeFoundation")]
public sealed class Audit_InitialDurableAdministrativeFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "audit");
        migrationBuilder.CreateTable(
            name: "audit_record",
            schema: "audit",
            columns: table => new
            {
                audit_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                evidence_id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                actor_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                action = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                resource_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                result = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                trace_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                reason_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                metadata = table.Column<string>(type: "jsonb", nullable: true),
                schema_version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_audit_record", x => x.audit_record_id));
        migrationBuilder.CreateIndex(name: "ux_audit_record_evidence_id", schema: "audit", table: "audit_record", column: "evidence_id", unique: true);
        migrationBuilder.CreateIndex(name: "ix_audit_record_actor_occurred_at", schema: "audit", table: "audit_record", columns: new[] { "actor_user_account_id", "occurred_at" });
        migrationBuilder.CreateIndex(name: "ix_audit_record_action_occurred_at", schema: "audit", table: "audit_record", columns: new[] { "action", "occurred_at" });
        migrationBuilder.CreateIndex(name: "ix_audit_record_resource_occurred_at", schema: "audit", table: "audit_record", columns: new[] { "resource_type", "resource_id", "occurred_at" });
        migrationBuilder.CreateIndex(name: "ix_audit_record_unit_occurred_at", schema: "audit", table: "audit_record", columns: new[] { "unit_id", "occurred_at" }, filter: "unit_id IS NOT NULL");
        migrationBuilder.CreateIndex(name: "ix_audit_record_correlation_id", schema: "audit", table: "audit_record", column: "correlation_id");
        migrationBuilder.CreateIndex(name: "ix_audit_record_occurred_at", schema: "audit", table: "audit_record", column: "occurred_at");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "audit_record", schema: "audit");
}
