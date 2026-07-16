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
    // Ciclo de vida da competência DEPOIS do nascimento (o nascimento está em DocumentGenerationFlowTests):
    // a unidade nasce na competência mínima quando não há data, e é o UpdateDocumentUnitDetails — o fluxo real,
    // HTTP -> Application -> DocumentService -> Domain -> EF — que a move para a competência da data informada.
    // Cobre também a retroatividade (UsePreviousPeriod), a troca de competência, a invalidação de pendências
    // duplicadas na mesma competência e o congelamento (editar o template não reescreve documentos existentes).
    //
    // Os templates aqui têm SÓ a PeriodPolicy: sem vencimento (o setter de Validity recusa validade no passado,
    // o que proibiria datas fixas) e sem carga horária (que exigiria dia útil) — as datas de 2024 ficam estáveis
    // e o teste isola a competência.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentPeriodLifecycleTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly MarchDate = new(2024, 3, 15);
        private static readonly DateOnly AprilDate = new(2024, 4, 10);

        [Fact]
        public async Task UpdateUnitDetails_UnitAtMinimumPeriod_MovesToTheDatePeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            // Pré-condição do cenário: sem data, a unidade nasceu na competência mínima.
            var before = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(Period.MIN_YEAR, Assert.Single(before.DocumentsUnits).Period!.Year);

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.NotNull(unit.Period);
            Assert.True(unit.Period!.IsMonthly);
            Assert.Equal(2024, unit.Period.Year);
            Assert.Equal(3, unit.Period.Month);
        }

        // Retroatividade ponta a ponta: com UsePreviousPeriod, a data de 15/03 situa o documento na competência
        // de FEVEREIRO — o documento emitido em março vale pelo mês anterior.
        [Fact]
        public async Task UpdateUnitDetails_TemplateUsesPreviousPeriod_UnitLandsOnThePriorPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: true);

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(2024, unit.Period!.Year);
            Assert.Equal(2, unit.Period.Month);
        }

        // A competência acompanha a data: um segundo update com data de outra competência move a unidade —
        // ela não fica presa à primeira.
        [Fact]
        public async Task UpdateUnitDetails_DateInAnotherPeriod_MovesTheUnit()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, AprilDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(2024, unit.Period!.Year);
            Assert.Equal(4, unit.Period.Month);
        }

        // Dedup por competência pelo fluxo real (hoje só coberto no unitário): quando o update coloca a unidade
        // na competência onde JÁ existe outra Pending, a outra é invalidada — cada competência espera um documento.
        [Fact]
        public async Task UpdateUnitDetails_SamePeriodAsAnotherPending_InvalidatesTheDuplicate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            // Segunda pendência já situada em março/2024 (data de referência na criação).
            var trackedDocument = await context.Documents.Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.Id == seed.DocumentId, ct);
            var marchUnit = trackedDocument.NewDocumentUnit(Guid.NewGuid(), MarchDate.ToDateTime(TimeOnly.MinValue));
            await context.SaveChangesAsync(ct);

            // O update move a unidade da competência mínima para março — a mesma da outra Pending.
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);
            Assert.Equal(DocumentUnitStatus.Pending, result.DocumentsUnits.First(u => u.Id == seed.UnitId).Status);
            Assert.Equal(DocumentUnitStatus.Invalid, result.DocumentsUnits.First(u => u.Id == marchUnit.Id).Status);
        }

        // O congelamento, ponta a ponta: a competência é copiada do template no NASCIMENTO do documento. Editar o
        // template depois (Monthly -> Yearly) não reescreve o documento — o update seguinte ainda situa a unidade
        // numa competência MENSAL. Sem isso, mudar o template corromperia o histórico de documentos emitidos.
        [Fact]
        public async Task EditTemplatePeriod_AfterDocumentExists_DoesNotRewriteTheDocument()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            // Edita o template para competência ANUAL (conjunto explícito de policies).
            var editClient = CreateClient();
            editClient.InputHeaders([seed.CompanyId]);
            var editCommand = new EditDocumentTemplateModel(
                seed.TemplateId, "Period Only", "Description Period Only",
                TemplateFileInfo: null,
                null, null, false, [], seed.DocumentGroupId, false,
                new PoliciesModel(Period: new PeriodPolicyModel(PeriodType.Yearly.Id, UsePreviousPeriod: false)));
            var editResponse = await editClient.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/documenttemplate", editCommand);
            Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(PeriodType.Monthly, result.PeriodType);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.True(unit.Period!.IsMonthly);
            Assert.Equal(2024, unit.Period.Year);
            Assert.Equal(3, unit.Period.Month);
        }

        private sealed record PeriodDocumentSeed(
            Guid CompanyId, Guid DocumentId, Guid UnitId, Guid EmployeeId, Guid TemplateId, Guid DocumentGroupId);

        // Seed do cenário-base: template SÓ com PeriodPolicy mensal (sem arquivo — o update pula a recuperação de
        // conteúdo), RequireDocuments por cargo sem evento, e a geração real via serviço — a unidade nasce SEM
        // data, na competência mínima.
        private async Task<PeriodDocumentSeed> SeedGeneratedPeriodDocumentAsync(
            PeopleManagementContext context, CancellationToken ct, bool usePreviousPeriod)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Period Only", "Description Period Only", company.Id,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: false, placeSignatures: [], documentGroupId: documentGroup.Id,
                usePreviousPeriod: false,
                policies: [new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod)]);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Period Doc", "Period Description", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(requireDocuments.Id, company.Id, ct);
            }

            var document = await GetDocumentForTemplateAsync(employee.Id, template.Id, ct);
            var unit = Assert.Single(document.DocumentsUnits);
            return new PeriodDocumentSeed(company.Id, document.Id, unit.Id, employee.Id, template.Id, documentGroup.Id);
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

        private async Task<Document> GetDocumentForTemplateAsync(Guid employeeId, Guid templateId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            return await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.EmployeeId == employeeId && x.DocumentTemplateId == templateId, ct);
        }
    }
}
