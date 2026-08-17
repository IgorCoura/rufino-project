using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;
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
    // duplicadas na mesma competência e a leitura ao vivo (editar o template vale imediatamente para as
    // próximas operações; a competência já gravada nas units é história e não muda sozinha).
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

            // Segunda pendência já situada em março/2024 (data de referência na criação). A configuração de
            // competência é passada por operação — aqui o teste faz o papel do caller que leu o template.
            var trackedDocument = await context.Documents.Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.Id == seed.DocumentId, ct);
            var marchUnit = trackedDocument.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, MarchDate.ToDateTime(TimeOnly.MinValue));
            await context.SaveChangesAsync(ct);

            // O update move a unidade da competência mínima para março — a mesma da outra Pending.
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, MarchDate));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);
            Assert.Equal(DocumentUnitStatus.Pending, result.DocumentsUnits.First(u => u.Id == seed.UnitId).Status);
            Assert.Equal(DocumentUnitStatus.Invalid, result.DocumentsUnits.First(u => u.Id == marchUnit.Id).Status);
        }

        // Leitura ao vivo, ponta a ponta: a competência NÃO é congelada no documento — toda operação lê a
        // PeriodPolicy atual do template. Editar o template (Monthly -> Yearly) vale imediatamente: o update
        // seguinte situa a unidade numa competência ANUAL. As competências já gravadas em units entregues são
        // história por unidade e não mudam (coberto em DocumentPeriodLiveReadTests).
        [Fact]
        public async Task EditTemplatePeriod_AfterDocumentExists_AppliesToTheNextOperations()
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
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.True(unit.Period!.IsYearly);
            Assert.Equal(2024, unit.Period.Year);
        }

        // --- Criação manual de uma competência ---------------------------------

        // POST /document com data: o RH preenchendo à mão a competência que ficou sem unidade. A unidade nasce
        // já situada na competência da data E com a data gravada — não é "esperando data" como a criação avulsa.
        [Fact]
        public async Task PostDocument_WithADate_CreatesTheUnitOnThatCompetency()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            var response = await PostDocumentAsync(seed, MarchDate);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);

            var created = result.DocumentsUnits.First(u => u.Id != seed.UnitId);
            Assert.Equal(DocumentUnitStatus.Pending, created.Status);
            Assert.Equal(MarchDate, created.Date);
            Assert.True(created.Period!.IsMonthly);
            Assert.Equal(2024, created.Period.Year);
            Assert.Equal(3, created.Period.Month);
        }

        [Fact]
        public async Task PostDocument_WithADateOnATemplateThatUsesPreviousPeriod_LandsOnThePriorCompetency()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: true);

            await PostDocumentAsync(seed, MarchDate);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var created = result.DocumentsUnits.First(u => u.Id != seed.UnitId);
            Assert.Equal(2, created.Period!.Month);
        }

        // A recusa é o ponto do fluxo: a segunda criação na mesma competência não devolve a unidade existente em
        // silêncio (como faz a criação avulsa) — ela para, e a mensagem manda depreciar ou invalidar a de lá.
        [Fact]
        public async Task PostDocument_WhenTheCompetencyIsAlreadyOccupied_IsRejected()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            await PostDocumentAsync(seed, MarchDate);
            var response = await PostDocumentAsync(seed, MarchDate);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("PMD.DOC27", await response.Content.ReadAsStringAsync());

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);
        }

        // Documento sem competência não aceita a criação manual: duas unidades não podem cobrir ao mesmo tempo,
        // e a próxima nasce de depreciar/invalidar a vigente ou de renovar.
        [Fact]
        public async Task PostDocument_WithADateOnANonPeriodicTemplate_IsRejected()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false, periodic: false);

            var response = await PostDocumentAsync(seed, MarchDate);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("PMD.DOC28", await response.Content.ReadAsStringAsync());

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Single(result.DocumentsUnits);
        }

        // Sem data, o caminho antigo (app legado) segue intacto: a pendente que espera data é reaproveitada em
        // vez de virar uma segunda pendência.
        [Fact]
        public async Task PostDocument_WithoutADate_StillReusesThePendingWaitingForADate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedPeriodDocumentAsync(context, ct, usePreviousPeriod: false);

            var response = await PostDocumentAsync(seed, date: null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(seed.UnitId, unit.Id);
            Assert.Equal(Period.MIN_YEAR, unit.Period!.Year);
        }

        private sealed record PeriodDocumentSeed(
            Guid CompanyId, Guid DocumentId, Guid UnitId, Guid EmployeeId, Guid TemplateId, Guid DocumentGroupId);

        // Seed do cenário-base: template SÓ com PeriodPolicy mensal (sem arquivo — o update pula a recuperação de
        // conteúdo), RequireDocuments por cargo sem evento, e a geração real via serviço — a unidade nasce SEM
        // data, na competência mínima.
        private async Task<PeriodDocumentSeed> SeedGeneratedPeriodDocumentAsync(
            PeopleManagementContext context, CancellationToken ct, bool usePreviousPeriod, bool periodic = true)
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
                policies: periodic ? [new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod)] : []);
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

        private async Task<HttpResponseMessage> PostDocumentAsync(PeriodDocumentSeed seed, DateOnly? date)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);
            return await client.PostAsJsonAsync($"/api/v1/{seed.CompanyId}/document",
                new CreateDocumentModel(seed.DocumentId, seed.EmployeeId, date));
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
