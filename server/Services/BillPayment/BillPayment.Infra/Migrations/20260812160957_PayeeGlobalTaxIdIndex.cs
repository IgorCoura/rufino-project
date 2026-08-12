using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PayeeGlobalTaxIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_payees_tax_id_global",
                schema: "bill_payment",
                table: "payees",
                column: "tax_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payees_tax_id_global",
                schema: "bill_payment",
                table: "payees");
        }
    }
}
