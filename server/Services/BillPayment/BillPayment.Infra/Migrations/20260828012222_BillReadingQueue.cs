using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class BillReadingQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "reading_arrived_after_decision",
                schema: "bill_payment",
                table: "bills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "reading_attempts",
                schema: "bill_payment",
                table: "bills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "reading_lease_expires_at",
                schema: "bill_payment",
                table: "bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reading_state",
                schema: "bill_payment",
                table: "bills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // BACKFILL — o default de `reading_state` e 0, que nao e valor nenhum do Smart Enum
            // e faria `Enumeration.FromValue` estourar na leitura de TODO boleto existente.
            //
            // A regra e a mesma do `InitialReadingState`: quem ja tem retrato nasce `Done` (3);
            // quem tem documento guardado e nao tem retrato entra na fila (`Queued` = 2) e se
            // resolve sozinho; quem nao tem documento nao tem o que ler (`NotApplicable` = 1).
            // Sao estes ultimos que hoje exigiriam `POST /bills/{id}/enrich` um a um.
            migrationBuilder.Sql("""
                UPDATE bill_payment.bills
                SET reading_state = CASE
                    WHEN reading IS NOT NULL THEN 3
                    WHEN origin_storage_key IS NOT NULL AND origin_storage_key <> '' THEN 2
                    ELSE 1
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_bills_reading_queue",
                schema: "bill_payment",
                table: "bills",
                columns: new[] { "reading_state", "reading_lease_expires_at" },
                filter: "reading_state = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bills_reading_queue",
                schema: "bill_payment",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "reading_arrived_after_decision",
                schema: "bill_payment",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "reading_attempts",
                schema: "bill_payment",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "reading_lease_expires_at",
                schema: "bill_payment",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "reading_state",
                schema: "bill_payment",
                table: "bills");
        }
    }
}
