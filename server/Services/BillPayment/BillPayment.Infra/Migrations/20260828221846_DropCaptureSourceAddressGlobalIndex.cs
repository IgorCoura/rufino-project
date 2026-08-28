using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class DropCaptureSourceAddressGlobalIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_capture_sources_address_global",
                schema: "bill_payment",
                table: "capture_sources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_capture_sources_address_global",
                schema: "bill_payment",
                table: "capture_sources",
                column: "address");
        }
    }
}
