using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PaymentOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "payment_order_id",
                schema: "bill_payment",
                table: "bills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_orders",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rail = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    hold = table.Column<int>(type: "integer", nullable: false),
                    requested_schedule_date = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_schedule_date = table.Column<DateOnly>(type: "date", nullable: true),
                    provider_order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_currency = table.Column<int>(type: "integer", nullable: true),
                    fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    fee_currency = table.Column<int>(type: "integer", nullable: true),
                    paid_at = table.Column<DateOnly>(type: "date", nullable: true),
                    fail_reasons = table.Column<string>(type: "jsonb", nullable: false),
                    last_provider_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    receipt_storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submission_attempts = table.Column<int>(type: "integer", nullable: false),
                    submission_lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_orders", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_orders_bill_active",
                schema: "bill_payment",
                table: "payment_orders",
                column: "bill_id",
                unique: true,
                filter: "\"status\" NOT IN (5, 6, 7)");

            migrationBuilder.CreateIndex(
                name: "ix_payment_orders_submission_queue",
                schema: "bill_payment",
                table: "payment_orders",
                columns: new[] { "status", "hold", "submission_lease_expires_at" },
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_payment_orders_tenant_created",
                schema: "bill_payment",
                table: "payment_orders",
                columns: new[] { "tenant_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_orders",
                schema: "bill_payment");

            migrationBuilder.DropColumn(
                name: "payment_order_id",
                schema: "bill_payment",
                table: "bills");
        }
    }
}
