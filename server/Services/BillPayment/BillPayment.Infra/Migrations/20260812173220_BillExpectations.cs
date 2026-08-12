using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class BillExpectations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bill_expectations",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recurrence = table.Column<int>(type: "integer", nullable: false),
                    expected_due_day = table.Column<int>(type: "integer", nullable: false),
                    observed_lead_days = table.Column<int>(type: "integer", nullable: false),
                    alert_lead_days = table.Column<int>(type: "integer", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    observation_count = table.Column<int>(type: "integer", nullable: false),
                    hint_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    paused_until = table.Column<DateOnly>(type: "date", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_expectations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bill_expectation_cycles",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competence = table.Column<int>(type: "integer", nullable: false),
                    expected_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    alert_at = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    fulfilled_by_bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blocked_by_capture_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    miss_reason = table.Column<int>(type: "integer", nullable: true),
                    waived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    waive_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alerts = table.Column<string>(type: "jsonb", nullable: false),
                    bill_expectation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_expectation_cycles", x => x.id);
                    table.ForeignKey(
                        name: "FK_bill_expectation_cycles_bill_expectations_bill_expectation_~",
                        column: x => x.bill_expectation_id,
                        principalSchema: "bill_payment",
                        principalTable: "bill_expectations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectation_cycles_expectation_competence",
                schema: "bill_payment",
                table: "bill_expectation_cycles",
                columns: new[] { "bill_expectation_id", "competence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_active_updated",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "is_active", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_tenant_payee_account",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "tenant_id", "payee_id", "account_reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bill_expectation_cycles",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "bill_expectations",
                schema: "bill_payment");
        }
    }
}
