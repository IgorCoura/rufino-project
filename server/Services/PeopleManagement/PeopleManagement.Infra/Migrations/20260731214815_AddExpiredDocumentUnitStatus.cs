using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <summary>
    /// Introduz o status Vencido (DocumentUnitStatus.Expired = 9) e o contador de vencimentos do documento.
    ///
    /// Antes desta migration, "venceu" e "foi substituído" eram o mesmo status (Deprecated), e cada consumidor
    /// reconstruía a diferença do seu jeito — o dashboard por subquery, o status do documento não reconstruía de
    /// jeito nenhum. O backfill materializa a distinção usando exatamente o predicado que o dashboard usava.
    /// </summary>
    public partial class AddExpiredDocumentUnitStatus : Migration
    {
        // DocumentUnitStatus: 2=OK, 3=Deprecated, 4=Invalid, 5=RequiresValidation, 6=NotApplicable,
        // 7=AwaitingSignature, 8=Warning, 9=Expired.
        //
        // Só converte Deprecated. Invalid entrava no bucket "Vencidos" do dashboard antigo, mas pela definição
        // nova é erro, não vencimento — fica como está.
        private const string BACKFILL_EXPIRED = """
            UPDATE people_management."DocumentsUnits" u
            SET "Status" = 9
            WHERE u."Status" = 3
              AND NOT EXISTS (
                  SELECT 1
                  FROM people_management."DocumentsUnits" o
                  WHERE o."DocumentId" = u."DocumentId"
                    AND o."Id" <> u."Id"
                    AND (o."Status" IN (2, 5, 6, 7, 8) OR (o."Status" IN (3, 4) AND o."Date" > u."Date"))
                    AND o."Period_Type"  IS NOT DISTINCT FROM u."Period_Type"
                    AND o."Period_Year"  IS NOT DISTINCT FROM u."Period_Year"
                    AND o."Period_Month" IS NOT DISTINCT FROM u."Period_Month"
                    AND o."Period_Day"   IS NOT DISTINCT FROM u."Period_Day"
                    AND o."Period_Week"  IS NOT DISTINCT FROM u."Period_Week"
              );
            """;

        // Conta quantas vezes cada documento efetivamente venceu, para que os documentos com renovação limitada
        // já em curso não recomecem a cota do zero.
        //
        // Só entram unidades que já saíram de vigência (Deprecated/Expired) E cujo prazo passou: unidade OK ou
        // Warning com validade vencida ainda vai ser processada pelo job e incrementaria de novo; unidade
        // inválida nunca venceu, foi erro. Unidade depreciada por substituição cujo prazo por acaso já tinha
        // passado é contada a mais — imprecisão aceita num backfill de dado legado, sendo que o contador passa a
        // ser exato daqui para a frente.
        private const string BACKFILL_EXPIRATION_COUNT = """
            UPDATE people_management."Documents" d
            SET "ExpirationCount" = sub.expiration_count
            FROM (
                SELECT u."DocumentId", COUNT(*) AS expiration_count
                FROM people_management."DocumentsUnits" u
                WHERE u."Validity" IS NOT NULL
                  AND u."Validity" < CURRENT_DATE
                  AND u."Status" IN (3, 9)
                GROUP BY u."DocumentId"
            ) sub
            WHERE d."Id" = sub."DocumentId";
            """;

        private const string REVERT_EXPIRED = """
            UPDATE people_management."DocumentsUnits" SET "Status" = 3 WHERE "Status" = 9;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpirationCount",
                schema: "people_management",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Ordem importa: o contador filtra por status 9, que só existe depois do primeiro backfill.
            migrationBuilder.Sql(BACKFILL_EXPIRED);
            migrationBuilder.Sql(BACKFILL_EXPIRATION_COUNT);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(REVERT_EXPIRED);

            migrationBuilder.DropColumn(
                name: "ExpirationCount",
                schema: "people_management",
                table: "Documents");
        }
    }
}
