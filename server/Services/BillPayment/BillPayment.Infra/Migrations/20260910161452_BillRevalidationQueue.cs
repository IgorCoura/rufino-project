using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class BillRevalidationQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "revalidation_attempts",
                schema: "bill_payment",
                table: "bills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "revalidation_next_attempt_at",
                schema: "bill_payment",
                table: "bills",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "revalidation_attempts",
                schema: "bill_payment",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "revalidation_next_attempt_at",
                schema: "bill_payment",
                table: "bills");
        }
    }
}
