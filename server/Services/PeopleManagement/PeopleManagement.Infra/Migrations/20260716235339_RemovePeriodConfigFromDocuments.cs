using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RemovePeriodConfigFromDocuments : Migration
    {
        // A competência deixou de ser copiada/congelada no Document: toda operação lê a PeriodPolicy atual do
        // template ("template é a configuração, a unit é a história"). Sem backfill algum — os documentos
        // legados com essas colunas nulas passam a funcionar automaticamente, porque a migration
        // DeriveDocumentTemplatePeriodPolicies já derivou a policy nos templates. As competências já gravadas
        // vivem em DocumentsUnits.Period e não são tocadas.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodType",
                schema: "people_management",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "UsePreviousPeriod",
                schema: "people_management",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodType",
                schema: "people_management",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsePreviousPeriod",
                schema: "people_management",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
