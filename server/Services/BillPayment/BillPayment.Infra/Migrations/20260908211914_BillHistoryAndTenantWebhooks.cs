using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class BillHistoryAndTenantWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "provider_authorized",
                schema: "bill_payment",
                table: "payment_orders",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_raw_status",
                schema: "bill_payment",
                table: "payment_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_transfer_id",
                schema: "bill_payment",
                table: "payment_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "requested_by",
                schema: "bill_payment",
                table: "payment_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asaas_webhook_id",
                schema: "bill_payment",
                table: "payer_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asaas_webhook_ref",
                schema: "bill_payment",
                table: "payer_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_webhook_event_at",
                schema: "bill_payment",
                table: "payer_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bill_history_entries",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<int>(type: "integer", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_status = table.Column<int>(type: "integer", nullable: true),
                    to_status = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_history_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_bill_history_entries_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "bill_payment",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_orders_provider_transfer",
                schema: "bill_payment",
                table: "payment_orders",
                column: "provider_transfer_id",
                filter: "provider_transfer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bill_history_bill_occurred",
                schema: "bill_payment",
                table: "bill_history_entries",
                columns: new[] { "bill_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bill_history_entries",
                schema: "bill_payment");

            migrationBuilder.DropIndex(
                name: "ix_payment_orders_provider_transfer",
                schema: "bill_payment",
                table: "payment_orders");

            migrationBuilder.DropColumn(
                name: "provider_authorized",
                schema: "bill_payment",
                table: "payment_orders");

            migrationBuilder.DropColumn(
                name: "provider_raw_status",
                schema: "bill_payment",
                table: "payment_orders");

            migrationBuilder.DropColumn(
                name: "provider_transfer_id",
                schema: "bill_payment",
                table: "payment_orders");

            migrationBuilder.DropColumn(
                name: "requested_by",
                schema: "bill_payment",
                table: "payment_orders");

            migrationBuilder.DropColumn(
                name: "asaas_webhook_id",
                schema: "bill_payment",
                table: "payer_profiles");

            migrationBuilder.DropColumn(
                name: "asaas_webhook_ref",
                schema: "bill_payment",
                table: "payer_profiles");

            migrationBuilder.DropColumn(
                name: "last_webhook_event_at",
                schema: "bill_payment",
                table: "payer_profiles");
        }
    }
}
