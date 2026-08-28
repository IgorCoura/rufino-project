using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ScopedIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_client_requests",
                schema: "bill_payment",
                table: "client_requests");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "bill_payment",
                table: "client_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_client_requests",
                schema: "bill_payment",
                table: "client_requests",
                columns: new[] { "tenant_id", "id", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_client_requests",
                schema: "bill_payment",
                table: "client_requests");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "bill_payment",
                table: "client_requests");

            migrationBuilder.AddPrimaryKey(
                name: "PK_client_requests",
                schema: "bill_payment",
                table: "client_requests",
                column: "id");
        }
    }
}
