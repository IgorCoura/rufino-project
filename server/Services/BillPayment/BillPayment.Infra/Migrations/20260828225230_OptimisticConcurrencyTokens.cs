using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class OptimisticConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "bill_payment",
                table: "capture_sources",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "bill_payment",
                table: "capture_items",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "bill_payment",
                table: "bills",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "bill_payment",
                table: "bill_expectations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "bill_payment",
                table: "capture_sources");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "bill_payment",
                table: "capture_items");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "bill_payment",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "bill_payment",
                table: "bill_expectations");
        }
    }
}
