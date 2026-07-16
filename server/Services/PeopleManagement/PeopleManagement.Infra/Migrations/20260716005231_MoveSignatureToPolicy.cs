using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class MoveSignatureToPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A assinatura virou policy: presença da SignaturePolicy = o template aceita assinatura, e os locais
            // moram dentro dela. Sem esta migração de dados os DROPs abaixo apagariam toda a configuração de
            // assinatura — o EF gera só o DDL, ele não sabe para onde os dados foram.
            //
            // Só templates com AcceptsSignature = true ganham policy. Quem tem local sem aceitar não deveria
            // existir (a invariante antiga barrava) e, se existir, é estado inválido que não deve ser preservado.
            // COALESCE '[]' cobre aceitar assinatura sem definir local, que é legítimo.
            //
            // As chaves espelham PlaceSignatureParams do DocumentPolicyFactory — se aquele record mudar, este
            // SQL precisa mudar junto.
            migrationBuilder.Sql("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT dt."Id",
                       5,
                       jsonb_build_object('PlaceSignatures', COALESCE((
                           SELECT jsonb_agg(jsonb_build_object(
                               'TypeId', ps."Type",
                               'Page', ps."Page",
                               'RelativePositionBotton', ps."RelativePositionBotton",
                               'RelativePositionLeft', ps."RelativePositionLeft",
                               'RelativeSizeX', ps."RelativeSizeX",
                               'RelativeSizeY', ps."RelativeSizeY"))
                           FROM people_management."PlaceSignature" ps
                           WHERE ps."DocumentTemplateId" = dt."Id"), '[]'::jsonb))
                FROM people_management."DocumentTemplates" dt
                WHERE dt."AcceptsSignature" = true;
                """);

            migrationBuilder.DropTable(
                name: "PlaceSignature",
                schema: "people_management");

            migrationBuilder.DropColumn(
                name: "AcceptsSignature",
                schema: "people_management",
                table: "DocumentTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptsSignature",
                schema: "people_management",
                table: "DocumentTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PlaceSignature",
                schema: "people_management",
                columns: table => new
                {
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Page = table.Column<double>(type: "double precision", nullable: false),
                    RelativePositionBotton = table.Column<double>(type: "double precision", nullable: false),
                    RelativePositionLeft = table.Column<double>(type: "double precision", nullable: false),
                    RelativeSizeX = table.Column<double>(type: "double precision", nullable: false),
                    RelativeSizeY = table.Column<double>(type: "double precision", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceSignature", x => new { x.DocumentTemplateId, x.Id });
                    table.ForeignKey(
                        name: "FK_PlaceSignature_DocumentTemplates_DocumentTemplateId",
                        column: x => x.DocumentTemplateId,
                        principalSchema: "people_management",
                        principalTable: "DocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Recria a estrutura sem os dados seria uma reversão que perde configuração de assinatura, então o
            // Down desfaz a migração de dados também: policy -> coluna + tabela, e some com a policy.
            migrationBuilder.Sql("""
                UPDATE people_management."DocumentTemplates" dt
                SET "AcceptsSignature" = true
                WHERE EXISTS (
                    SELECT 1 FROM people_management."DocumentTemplatePolicies" p
                    WHERE p."DocumentTemplateId" = dt."Id" AND p."Type" = 5);
                """);

            migrationBuilder.Sql("""
                INSERT INTO people_management."PlaceSignature"
                    ("DocumentTemplateId", "Type", "Page", "RelativePositionBotton", "RelativePositionLeft", "RelativeSizeX", "RelativeSizeY")
                SELECT p."DocumentTemplateId",
                       (e->>'TypeId')::int,
                       (e->>'Page')::double precision,
                       (e->>'RelativePositionBotton')::double precision,
                       (e->>'RelativePositionLeft')::double precision,
                       (e->>'RelativeSizeX')::double precision,
                       (e->>'RelativeSizeY')::double precision
                FROM people_management."DocumentTemplatePolicies" p,
                     jsonb_array_elements(p."Params"->'PlaceSignatures') e
                WHERE p."Type" = 5;
                """);

            migrationBuilder.Sql("""
                DELETE FROM people_management."DocumentTemplatePolicies" WHERE "Type" = 5;
                """);
        }
    }
}
