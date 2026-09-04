using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ExpectationLeadSplitAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bill_expectations_active_updated",
                schema: "bill_payment",
                table: "bill_expectations");

            migrationBuilder.AddColumn<int>(
                name: "anchor_competence",
                schema: "bill_payment",
                table: "bill_expectations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_swept_at",
                schema: "bill_payment",
                table: "bill_expectations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "watching_since",
                schema: "bill_payment",
                table: "bill_expectations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "tenant_notification_settings",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    recipients = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_notification_settings", x => x.id);
                });

            // BACKFILL — obrigatorio, nao cosmetico. O default de `anchor_competence` e 0, e
            // `CompetencePeriod` recusa ano 0 na releitura: sem isto TODA expectativa existente
            // quebraria ao ser carregada. A ancora sai da competencia do ciclo mais recente
            // (a melhor prova de onde a cadencia esta) e, sem ciclo, do mes de criacao.
            migrationBuilder.Sql("""
                UPDATE bill_payment.bill_expectations e
                SET anchor_competence = COALESCE(
                    (SELECT c.competence
                       FROM bill_payment.bill_expectation_cycles c
                      WHERE c.bill_expectation_id = e.id
                      ORDER BY c.competence DESC
                      LIMIT 1),
                    (EXTRACT(YEAR FROM e.created_at)::int * 100) + EXTRACT(MONTH FROM e.created_at)::int);
                """);

            // A vigilancia comeca quando a expectativa foi criada — e nunca antes. Deixar o
            // default 0001-01-01 faria a varredura abrir todo ciclo cuja data de alerta ja passou
            // e marca-lo como nao cumprido no mesmo instante: uma enxurrada de alerta falso na
            // primeira passagem depois do deploy.
            migrationBuilder.Sql("""
                UPDATE bill_payment.bill_expectations
                SET watching_since = created_at,
                    last_swept_at = created_at;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_active_swept",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "is_active", "last_swept_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_tenant_hint_source",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "tenant_id", "hint_source_id" },
                filter: "hint_source_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectation_cycles_blocked_item",
                schema: "bill_payment",
                table: "bill_expectation_cycles",
                column: "blocked_by_capture_item_id",
                filter: "blocked_by_capture_item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_notification_settings_tenant",
                schema: "bill_payment",
                table: "tenant_notification_settings",
                column: "tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_notification_settings",
                schema: "bill_payment");

            migrationBuilder.DropIndex(
                name: "ix_bill_expectations_active_swept",
                schema: "bill_payment",
                table: "bill_expectations");

            migrationBuilder.DropIndex(
                name: "ix_bill_expectations_tenant_hint_source",
                schema: "bill_payment",
                table: "bill_expectations");

            migrationBuilder.DropIndex(
                name: "ix_bill_expectation_cycles_blocked_item",
                schema: "bill_payment",
                table: "bill_expectation_cycles");

            migrationBuilder.DropColumn(
                name: "anchor_competence",
                schema: "bill_payment",
                table: "bill_expectations");

            migrationBuilder.DropColumn(
                name: "last_swept_at",
                schema: "bill_payment",
                table: "bill_expectations");

            migrationBuilder.DropColumn(
                name: "watching_since",
                schema: "bill_payment",
                table: "bill_expectations");

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_active_updated",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "is_active", "updated_at" });
        }
    }
}
