using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // Migration DeriveDocumentTemplatePeriodPolicies: deriva a PeriodPolicy dos templates existentes a partir da
    // frequência dos eventos que os alcançam, só quando não há ambiguidade. A suíte roda em banco novo, então o
    // cenário é montado à mão e o SQL da migration é reaplicado — é uma cópia do SQL da migration; se um mudar,
    // este teste falha e cobra o outro.
    //
    // O teste também guarda contra o erro de ler a tabela errada: o vínculo mora em RequireDocuments.
    // DocumentsTemplatesIds (uuid[]), não na tabela de junção DocumentTemplateRequireDocuments (nunca populada).
    [Collection(nameof(IntegrationTestCollection))]
    public class DerivePeriodPolicyMigrationTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private const string DeriveSql = """
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
            """;

        [Fact]
        public async Task Derive_WhenTemplateReachedBySingleFrequency_CreatesMatchingPeriodPolicy()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, role) = await SeedCompanyAndRoleAsync(context, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct);
            await context.SaveChangesAsync(ct);

            await LinkTemplateToEventsAsync(context, company.Id, role.Id, template.Id, [RecurringEvents.MONTHLY], ct);

            await context.Database.ExecuteSqlRawAsync(DeriveSql, ct);

            var persisted = await ReadTemplateAsync(template.Id, ct);
            var period = persisted.GetPolicy<IPeriodPolicy>();
            Assert.NotNull(period);
            Assert.Equal(PeriodType.Monthly, period!.PeriodType);
        }

        [Fact]
        public async Task Derive_WhenTemplateReachedByConflictingFrequencies_CreatesNoPolicy()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, role) = await SeedCompanyAndRoleAsync(context, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct);
            await context.SaveChangesAsync(ct);

            // Dois RequireDocuments distintos alcançam o mesmo template com frequências diferentes — a validação
            // ListenEventsIsValid só barra divergência dentro de um mesmo RequireDocuments, não entre dois.
            await LinkTemplateToEventsAsync(context, company.Id, role.Id, template.Id, [RecurringEvents.MONTHLY], ct);
            await LinkTemplateToEventsAsync(context, company.Id, role.Id, template.Id, [RecurringEvents.YEARLY], ct);

            await context.Database.ExecuteSqlRawAsync(DeriveSql, ct);

            var persisted = await ReadTemplateAsync(template.Id, ct);
            Assert.False(persisted.HasPolicy<IPeriodPolicy>());
        }

        [Fact]
        public async Task Derive_WhenTemplateHasNoRecurringEvent_CreatesNoPolicy()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, role) = await SeedCompanyAndRoleAsync(context, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct);
            await context.SaveChangesAsync(ct);

            // Evento não recorrente (admissão) não implica competência.
            await LinkTemplateToEventsAsync(context, company.Id, role.Id, template.Id, [EmployeeEvent.COMPLETE_ADMISSION_EVENT], ct);

            await context.Database.ExecuteSqlRawAsync(DeriveSql, ct);

            var persisted = await ReadTemplateAsync(template.Id, ct);
            Assert.False(persisted.HasPolicy<IPeriodPolicy>());
        }

        private static async Task<(Domain.AggregatesModel.CompanyAggregate.Company Company, Role Role)> SeedCompanyAndRoleAsync(
            PeopleManagementContext context, CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            return (company, role);
        }

        private static async Task LinkTemplateToEventsAsync(PeopleManagementContext context, Guid companyId, Guid roleId,
            Guid templateId, int[] eventIds, CancellationToken ct)
        {
            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), companyId, [roleId], AssociationType.Role,
                "Req", "Req Description",
                [.. eventIds.Select(e => ListenEvent.Create(e, [Status.Active.Id]))], [templateId]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);
        }

        private async Task<DocumentTemplate> ReadTemplateAsync(Guid templateId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var readContext = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            return await readContext.DocumentTemplates.AsNoTracking().FirstAsync(x => x.Id == templateId, ct);
        }
    }
}
