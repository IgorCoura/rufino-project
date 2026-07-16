using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infra.Migrations
{
    /// <inheritdoc />
    public partial class DeriveDocumentTemplatePeriodPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 3: a competência virou config do template (PeriodPolicy). Antes, ela era decidida pela
            // frequência do evento que disparava a geração. Aqui derivamos a policy dos templates existentes a
            // partir dos eventos que os alcançam, mas SÓ quando não há ambiguidade — um template alcançado por
            // eventos de frequências diferentes fica sem policy, para configuração manual (decisão do time).
            //
            // Mapa evento -> PeriodType espelha o antigo ConvertFrequencyToPeriodType:
            //   1013->Daily(1), 1014->Weekly(2), 1015->Monthly(3), 1016->Yearly(4), meses 1001-1012->Monthly(3).
            //   MINUTELY(1017) e não recorrentes não implicam competência.
            //
            // O jsonb e o discriminador (Type=2) espelham PeriodParams do DocumentPolicyFactory
            // ({PeriodTypeId, UsePreviousPeriod}); UsePreviousPeriod vem da coluna legada do template. Se aquele
            // record mudar, este SQL muda junto.
            // O vínculo template<->requireDoc mora na coluna uuid[] RequireDocuments."DocumentsTemplatesIds"
            // (a tabela DocumentTemplateRequireDocuments existe no modelo mas não é populada — não há navegação
            // que a preencha). Por isso o unnest do array, e não um join na tabela de junção.
            migrationBuilder.Sql("""
                INSERT INTO people_management."DocumentTemplatePolicies" ("DocumentTemplateId", "Type", "Params")
                SELECT t.template_id,
                       2,
                       jsonb_build_object('PeriodTypeId', t.period_type_id, 'UsePreviousPeriod', dt."UsePreviousPeriod")
                FROM (
                    SELECT link.template_id,
                           MIN(freq.period_type_id) AS period_type_id
                    FROM (
                        SELECT unnest(rd."DocumentsTemplatesIds") AS template_id, le."EventId" AS event_id
                        FROM people_management."RequireDocuments" rd
                        JOIN people_management."ListenEvent" le ON le."RequireDocumentsId" = rd."Id"
                    ) link
                    JOIN LATERAL (
                        SELECT CASE
                            WHEN link.event_id = 1013 THEN 1
                            WHEN link.event_id = 1014 THEN 2
                            WHEN link.event_id = 1015 THEN 3
                            WHEN link.event_id = 1016 THEN 4
                            WHEN link.event_id BETWEEN 1001 AND 1012 THEN 3
                            ELSE NULL
                        END AS period_type_id
                    ) freq ON freq.period_type_id IS NOT NULL
                    GROUP BY link.template_id
                    HAVING COUNT(DISTINCT freq.period_type_id) = 1
                ) t
                JOIN people_management."DocumentTemplates" dt ON dt."Id" = t.template_id
                WHERE NOT EXISTS (
                    SELECT 1 FROM people_management."DocumentTemplatePolicies" p
                    WHERE p."DocumentTemplateId" = t.template_id AND p."Type" = 2
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM people_management."DocumentTemplatePolicies" WHERE "Type" = 2;
                """);
        }
    }
}
