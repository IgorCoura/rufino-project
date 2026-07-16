using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTemplatePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTemplatePolicies",
                schema: "people_management",
                columns: table => new
                {
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Params = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplatePolicies", x => new { x.DocumentTemplateId, x.Id });
                    table.ForeignKey(
                        name: "FK_DocumentTemplatePolicies_DocumentTemplates_DocumentTemplate~",
                        column: x => x.DocumentTemplateId,
                        principalSchema: "people_management",
                        principalTable: "DocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backfill: deriva as policies dos templates já existentes a partir das colunas escalares legadas
            // (que permanecem como fonte da verdade nesta fase). Sem isso, um template anterior à migration
            // voltaria do banco sem policies. Os ticks reproduzem exatamente o payload que o
            // DocumentPolicyFactory grava — por isso o params viaja em ticks, e não como TimeSpan textual.
            //
            // O filtro > 0 espelha o guard de SyncPoliciesFromFields e a invariante das policies: coluna zerada
            // é ausência de regra, não regra com valor zero. Sem ele o backfill criaria linhas que o domínio
            // recusa a reidratar.
            migrationBuilder.Sql("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id",
                       1,
                       jsonb_build_object('DurationTicks', (EXTRACT(EPOCH FROM "DocumentValidityDuration") * 10000000)::bigint)
                FROM people_management."DocumentTemplates"
                WHERE "DocumentValidityDuration" > INTERVAL '0';
                """);

            migrationBuilder.Sql("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT "Id",
                       4,
                       jsonb_build_object('WorkloadTicks', (EXTRACT(EPOCH FROM "Workload") * 10000000)::bigint)
                FROM people_management."DocumentTemplates"
                WHERE "Workload" > INTERVAL '0';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentTemplatePolicies",
                schema: "people_management");
        }
    }
}
