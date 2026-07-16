using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RemoveZeroedDocumentTemplatePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A primeira versão do backfill de AddDocumentTemplatePolicies filtrava por IS NOT NULL, que não
            // exclui 00:00:00 — e a coluna guarda zero para todo template salvo por cliente que mandava 0 em vez
            // de null. O resultado foram linhas de policy com duração zerada.
            //
            // Depois disso as policies passaram a recusar duração não-positiva (PMD.DOCT11), então essas linhas
            // deixaram de ser lixo silencioso e passaram a derrubar a leitura do template: GetPolicy reidrata pelo
            // DocumentPolicyFactory e lança. O backfill já foi corrigido para filtrar > INTERVAL '0', mas quem
            // aplicou a migration antes disso não re-executa — daí esta limpeza.
            //
            // No-op em banco criado depois da correção.
            migrationBuilder.Sql("""
                DELETE FROM people_management."DocumentTemplatePolicies"
                WHERE ("Type" = 1 AND COALESCE(("Params"->>'DurationTicks')::bigint, 0) <= 0)
                   OR ("Type" = 4 AND COALESCE(("Params"->>'WorkloadTicks')::bigint, 0) <= 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem Down: recriar linhas que o domínio recusa a reidratar deixaria o banco pior do que antes.
        }
    }
}
