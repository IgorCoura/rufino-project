using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CaptureItemAccountReferenceMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "account_reference_suggestion",
                schema: "bill_payment",
                table: "capture_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "remembered_account_reference",
                schema: "bill_payment",
                table: "capture_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "account_reference_suggestion",
                schema: "bill_payment",
                table: "capture_items");

            migrationBuilder.DropColumn(
                name: "remembered_account_reference",
                schema: "bill_payment",
                table: "capture_items");
        }
    }
}
