using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PaymentOrderReceiptSweep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "receipt_unavailable",
                schema: "bill_payment",
                table: "payment_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "sweep_attempted_at",
                schema: "bill_payment",
                table: "payment_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "receipt_unavailable",
                schema: "bill_payment",
                table: "payment_orders");

            migrationBuilder.DropColumn(
                name: "sweep_attempted_at",
                schema: "bill_payment",
                table: "payment_orders");
        }
    }
}
