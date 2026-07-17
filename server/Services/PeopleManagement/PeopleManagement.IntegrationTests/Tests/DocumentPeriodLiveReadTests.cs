using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Application.Commands.DocumentTemplateCommands;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // A leitura AO VIVO da competência ("template é a configuração, a unit é a história") nos três cenários que
    // o congelamento não cobria — todos pelo fluxo real (HTTP/serviço -> Domain -> EF):
    //  1. Trocar a granularidade com uma pendente na competência mínima NÃO deixa órfã nem cria duplicada — a
    //     pendente é reaproveitada e re-situada na mínima do tipo novo.
    //  2. Documento nascido quando o template NÃO tinha PeriodPolicy "sara" sozinho: o template ganha a regra e
    //     o próximo update com data situa a unidade na competência (é o que salva os documentos legados, cujas
    //     colunas de competência do Document foram removidas sem backfill).
    //  3. Units entregues (OK) mantêm a competência gravada mesmo depois de o template trocar de granularidade —
    //     a configuração muda, a história não.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentPeriodLiveReadTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly MarchDate = new(2024, 3, 15);

        [Fact]
        public async Task EditTemplateGranularity_PendingAtMinimumPeriod_IsReusedAndResituatedNotOrphaned()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct,
                policies: [new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod: false)]);

            // Pré-condição: a unidade nasceu esperando data, na mínima MENSAL.
            var before = await GetDocumentAsync(seed.DocumentId, ct);
            var pending = Assert.Single(before.DocumentsUnits);
            Assert.True(pending.Period!.IsMonthly);
            Assert.Equal(Period.MIN_YEAR, pending.Period.Year);

            await EditTemplatePeriodAsync(seed, PeriodType.Yearly);

            // Nova geração com a granularidade nova: reaproveita a pendente (nada de órfã + duplicada) e a
            // re-situa na mínima ANUAL.
            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(seed.RequireDocumentsId, seed.CompanyId, ct);
            }

            var after = await GetDocumentAsync(seed.DocumentId, ct);
            var reused = Assert.Single(after.DocumentsUnits);
            Assert.Equal(pending.Id, reused.Id);
            Assert.True(reused.Period!.IsYearly);
            Assert.Equal(Period.MIN_YEAR, reused.Period.Year);

            // E quando a data real chega, a unidade cai na competência ANUAL da data.
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(reused.Id, seed.DocumentId, seed.EmployeeId, MarchDate));

            var final = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(final.DocumentsUnits);
            Assert.True(unit.Period!.IsYearly);
            Assert.Equal(2024, unit.Period.Year);
        }

        [Fact]
        public async Task TemplateGainsPeriodPolicy_ExistingDocumentIsSituatedOnTheNextUpdate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            // Documento nascido de um template SEM regras: a unidade não tem competência.
            var seed = await SeedGeneratedDocumentAsync(context, ct, policies: []);

            var before = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Null(Assert.Single(before.DocumentsUnits).Period);

            // O template ganha a PeriodPolicy depois do nascimento do documento.
            await EditTemplatePeriodAsync(seed, PeriodType.Monthly);

            // O próximo update com data situa a unidade — o documento legado "sara" sem migração de dados.
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.NotNull(unit.Period);
            Assert.True(unit.Period!.IsMonthly);
            Assert.Equal(2024, unit.Period.Year);
            Assert.Equal(3, unit.Period.Month);
        }

        [Fact]
        public async Task EditTemplateGranularity_DeliveredUnitsKeepTheirRecordedPeriods()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct,
                policies: [new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod: false)]);

            // Entrega na competência mensal de março/2024.
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));
            await MakeUnitOkAsync(seed.DocumentId, seed.UnitId, ct);

            await EditTemplatePeriodAsync(seed, PeriodType.Yearly);

            // Nova geração: nasce OUTRA pendente (anual, na mínima) — e a entregue não se move.
            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(seed.RequireDocumentsId, seed.CompanyId, ct);
            }

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);

            var delivered = result.DocumentsUnits.First(x => x.Id == seed.UnitId);
            Assert.Equal(DocumentUnitStatus.OK, delivered.Status);
            Assert.True(delivered.Period!.IsMonthly);
            Assert.Equal(2024, delivered.Period.Year);
            Assert.Equal(3, delivered.Period.Month);

            var newPending = Assert.Single(result.DocumentsUnits, x => x.Status == DocumentUnitStatus.Pending);
            Assert.True(newPending.Period!.IsYearly);
            Assert.Equal(Period.MIN_YEAR, newPending.Period.Year);
        }

        private sealed record LiveReadSeed(
            Guid CompanyId, Guid DocumentId, Guid UnitId, Guid EmployeeId, Guid TemplateId, Guid DocumentGroupId,
            Guid RequireDocumentsId, string TemplateName);

        // Template com o conjunto EXPLÍCITO de policies informado (sem arquivo), RequireDocuments por cargo sem
        // evento, e a geração real via serviço — a unidade nasce Pending, sem data.
        private async Task<LiveReadSeed> SeedGeneratedDocumentAsync(
            PeopleManagementContext context, CancellationToken ct, IEnumerable<IDocumentPolicy> policies)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Live Read", "Description Live Read", company.Id,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: false, placeSignatures: [], documentGroupId: documentGroup.Id,
                usePreviousPeriod: false,
                policies: policies);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Live Read Doc", "Live Read Description", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(requireDocuments.Id, company.Id, ct);
            }

            using var readScope = _factory.Services.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await readContext.Documents.AsNoTracking().Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.EmployeeId == employee.Id && x.DocumentTemplateId == template.Id, ct);
            var unit = Assert.Single(document.DocumentsUnits);
            return new LiveReadSeed(company.Id, document.Id, unit.Id, employee.Id, template.Id, documentGroup.Id,
                requireDocuments.Id, "Live Read");
        }

        // Edita o template via API trocando SÓ a regra de competência (conjunto explícito de policies).
        private async Task EditTemplatePeriodAsync(LiveReadSeed seed, PeriodType periodType)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);
            var command = new EditDocumentTemplateModel(
                seed.TemplateId, seed.TemplateName, $"Description {seed.TemplateName}",
                TemplateFileInfo: null,
                null, null, false, [], seed.DocumentGroupId, false,
                new PoliciesModel(Period: new PeriodPolicyModel(periodType.Id, UsePreviousPeriod: false)));
            var response = await client.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/documenttemplate", command);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }

        // Cliente novo por request: InputHeaders fixa o x-requestid, e reusar o cliente faria a idempotência
        // engolir o segundo PUT.
        private async Task PutUnitDetailsAsync(Guid companyId, UpdateDocumentUnitDetailsModel command)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{companyId}/document/documentunit", command);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }

        private async Task MakeUnitOkAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            document.InsertUnitWithoutRequireValidation(unitId, "file", "pdf");
            await context.SaveChangesAsync(ct);
        }
    }
}
