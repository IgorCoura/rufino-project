using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Net.Http.Headers;

namespace PeopleManagement.IntegrationTests.Tests
{
    // Verifica, de ponta a ponta (HTTP -> Application -> Domain -> EF -> Postgres), as transições de
    // DocumentStatus provocadas por mudanças de status das DocumentUnits, E a propagação SÍNCRONA
    // (via DocumentStatusChangedDomainEvent, despachado dentro do SaveChanges do UnitOfWork) para o
    // EmployeeDocumentStatus (Employee.DocumentRepresentingStatus).
    //
    // Mapeamento derivado (Document.RefreshDocumentStatus) e agregação por funcionário
    // (EmployeeDocumentStatusService.DetermineStatusFromDocumentStatuses):
    //   Document RequiresDocument/RequiresValidation -> Employee RequiresAttention
    //   Document Warning                             -> Employee Warning
    //   Document OK/AwaitingSignature/Deprecated     -> Employee Okay
    //
    // Cobertura de estados aqui:
    //   DocumentUnitStatus: Pending, NotApplicable, Invalid, OK, Warning, Deprecated.
    //   DocumentStatus:     RequiresDocument, OK, Warning, Deprecated.
    //   EmployeeDocumentStatus: Okay, RequiresAttention, Warning.
    // RequiresValidation e AwaitingSignature não têm caminho no ambiente de teste (não há endpoint que
    // chame InsertWithRequireValidation; a assinatura depende da ZapSign externa) — ficam cobertos pelos
    // testes unitários de DocumentStatusTransitionTests.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentStatusPropagationTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        // Data fixa (evita flakiness): está no passado, satisfazendo as guardas de data do domínio.
        private static readonly DateOnly OfficialDate = new(2024, 1, 15);

        // POST /document adiciona uma DocumentUnit Pending -> Document RequiresDocument -> Employee RequiresAttention;
        // depois PUT /document/DocumentUnit/not-applicable -> Document OK -> Employee volta para Okay.
        [Fact]
        public async Task DocumentUnitLifecycle_ThroughHttp_ShouldPropagateEmployeeStatusAtEachStep()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var scenario = await SeedScenarioAsync(context, associationIds: null, configureUnits: null, ct);

            Assert.Equal(EmployeeDocumentStatus.Okay, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));

            // Cliente novo por requisição: InputHeaders fixa um x-requestid no cliente e a idempotência
            // (IdentifiedCommand) descartaria a 2ª chamada como duplicada se reusássemos o mesmo cliente.
            var addUnitResponse = await _factory.CreateClient().InputHeaders([scenario.CompanyId]).PostAsJsonAsync(
                $"/api/v1/{scenario.CompanyId}/document",
                new { DocumentId = scenario.DocumentId, EmployeeId = scenario.EmployeeId });
            Assert.Equal(HttpStatusCode.OK, addUnitResponse.StatusCode);

            var afterAdd = await GetDocumentAsync(scenario.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Pending, Assert.Single(afterAdd.DocumentsUnits).Status);
            Assert.Equal(DocumentStatus.RequiresDocument, afterAdd.Status);
            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));

            var unitId = afterAdd.DocumentsUnits.First().Id;
            var notApplicableResponse = await _factory.CreateClient().InputHeaders([scenario.CompanyId]).PutAsJsonAsync(
                $"/api/v1/{scenario.CompanyId}/document/DocumentUnit/not-applicable",
                new { DocumentUnitId = unitId, DocumentId = scenario.DocumentId, EmployeeId = scenario.EmployeeId });
            Assert.Equal(HttpStatusCode.OK, notApplicableResponse.StatusCode);

            var afterNotApplicable = await GetDocumentAsync(scenario.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.NotApplicable, Assert.Single(afterNotApplicable.DocumentsUnits).Status);
            Assert.Equal(DocumentStatus.OK, afterNotApplicable.Status);
            Assert.Equal(EmployeeDocumentStatus.Okay, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));
        }

        // PUT /document/DocumentUnit/invalid: unit Pending -> Invalid; o Document permanece RequiresDocument
        // (Invalid não é tratado no GetStatusFromGroup) e o Employee segue RequiresAttention.
        [Fact]
        public async Task PutInvalid_WhenUnitIsPending_ShouldMakeUnitInvalidAndKeepEmployeeRequiresAttention()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var client = CreateClient();

            var scenario = await SeedScenarioAsync(context, associationIds: null, configureUnits: AddPendingUnit, ct);
            var unitId = scenario.Document.DocumentsUnits.First().Id;

            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));

            client.InputHeaders([scenario.CompanyId]);
            var response = await client.PutAsJsonAsync(
                $"/api/v1/{scenario.CompanyId}/document/DocumentUnit/invalid",
                new { DocumentUnitId = unitId, DocumentId = scenario.DocumentId, EmployeeId = scenario.EmployeeId });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var document = await GetDocumentAsync(scenario.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Invalid, Assert.Single(document.DocumentsUnits).Status);
            Assert.Equal(DocumentStatus.RequiresDocument, document.Status);
            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));
        }

        // POST /document/insert anexa um PDF sem exigir validação: unit Pending -> OK -> Document OK -> Employee Okay.
        [Fact]
        public async Task PostInsert_WhenUnitIsPending_ShouldMakeDocumentOkAndEmployeeOkay()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var client = CreateClient();

            var scenario = await SeedScenarioAsync(context, associationIds: null, configureUnits: AddPendingUnitWithDate, ct);
            var unitId = scenario.Document.DocumentsUnits.First().Id;

            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));

            using var content = new MultipartFormDataContent();
            var path = Path.Combine("DataForTests", "199f760b-601d-4a05-aee4-d0a9dbcc6b4d.pdf");
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "formFile", Path.GetFileName(path));
            content.Add(new StringContent(unitId.ToString()), "documentUnitId");
            content.Add(new StringContent(scenario.DocumentId.ToString()), "documentId");
            content.Add(new StringContent(scenario.EmployeeId.ToString()), "employeeId");

            client.InputHeaders([scenario.CompanyId]);
            var response = await client.PostAsync($"/api/v1/{scenario.CompanyId}/document/insert", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var document = await GetDocumentAsync(scenario.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.OK, Assert.Single(document.DocumentsUnits).Status);
            Assert.Equal(DocumentStatus.OK, document.Status);
            Assert.Equal(EmployeeDocumentStatus.Okay, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));
        }

        // Expiração com aviso (job Hangfire, chamado direto via IDocumentDepreciationService pois os workers
        // ficam desligados em teste): unit OK associada -> Warning -> Document Warning -> Employee Warning.
        [Fact]
        public async Task WarningExpiration_WhenUnitIsOkAndAssociated_ShouldMakeDocumentWarningAndEmployeeWarning()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var scenario = await SeedScenarioAsync(context, associationIds: null, configureUnits: AddOkUnit, ct);
            var unitId = scenario.Document.DocumentsUnits.First().Id;

            Assert.Equal(EmployeeDocumentStatus.Okay, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));

            using (var scope = _factory.Services.CreateScope())
            {
                var depreciationService = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
                await depreciationService.WarningExpirateDocument(unitId, scenario.DocumentId, scenario.CompanyId, ct);
            }

            var document = await GetDocumentAsync(scenario.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Warning, Assert.Single(document.DocumentsUnits).Status);
            Assert.Equal(DocumentStatus.Warning, document.Status);
            Assert.Equal(EmployeeDocumentStatus.Warning, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));
        }

        // Expiração/depreciação (job Hangfire) de documento sem associação vigente: unit OK -> Deprecated ->
        // Document Deprecated -> Employee volta para Okay (Deprecated não gera atenção nem aviso).
        [Fact]
        public async Task DepreciateExpiration_WhenUnitIsOkAndNotAssociated_ShouldMakeDocumentDeprecatedAndEmployeeOkay()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var scenario = await SeedScenarioAsync(context, associationIds: [Guid.NewGuid()], configureUnits: AddOkUnit, ct);
            var unitId = scenario.Document.DocumentsUnits.First().Id;

            Assert.Equal(EmployeeDocumentStatus.Okay, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));

            using (var scope = _factory.Services.CreateScope())
            {
                var depreciationService = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
                await depreciationService.DepreciateExpirateDocument(unitId, scenario.DocumentId, scenario.CompanyId, ct);
            }

            var document = await GetDocumentAsync(scenario.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Deprecated, Assert.Single(document.DocumentsUnits).Status);
            Assert.Equal(DocumentStatus.Deprecated, document.Status);
            Assert.Equal(EmployeeDocumentStatus.Okay, await GetEmployeeStatusAsync(scenario.EmployeeId, ct));
        }

        // -----------------------------------------------------------------
        // Seed & helpers
        // -----------------------------------------------------------------

        private sealed record SeededScenario(Guid CompanyId, Guid EmployeeId, Document Document)
        {
            public Guid DocumentId => Document.Id;
        }

        // Cria company/role/workplace/template/employee e, num segundo save, o RequireDocuments + um único
        // Document para o funcionário. O Employee é salvo ANTES de existir qualquer RequireDocuments no banco,
        // garantindo que os eventos de criação do funcionário não auto-provisionem documentos — o funcionário
        // fica com EXATAMENTE um Document (garantido pelo Assert de contagem), tornando a agregação determinística.
        private async Task<SeededScenario> SeedScenarioAsync(
            PeopleManagementContext context,
            List<Guid>? associationIds,
            Action<Document>? configureUnits,
            CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var workplace = await context.InsertWorkplace(company.Id, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct);
            await context.InsertArchiveCategory(company.Id, ct);

            var employee = Employee.Create(Guid.NewGuid(), company.Id, "Rosdevaldo Pereira", role.Id, workplace.Id);
            await context.Employees.AddAsync(employee, ct);
            await context.SaveChangesAsync(ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, associationIds ?? [role.Id],
                AssociationType.Role, "Doc Role Required", "Description Doc Role Required", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);

            var document = Document.Create(Guid.NewGuid(), employee.Id, company.Id, requireDocuments.Id, template.Id,
                template.Name.ToString(), template.Description.ToString());
            await context.Documents.AddAsync(document, ct);

            configureUnits?.Invoke(document);

            await context.SaveChangesAsync(ct);

            Assert.Equal(1, await context.Documents.CountAsync(x => x.EmployeeId == employee.Id, ct));

            return new SeededScenario(company.Id, employee.Id, document);
        }

        private static void AddPendingUnit(Document document)
            => document.NewDocumentUnit(Guid.NewGuid());

        private static void AddPendingUnitWithDate(Document document)
        {
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, OfficialDate, TimeSpan.Zero, "content");
        }

        private static void AddOkUnit(Document document)
        {
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, OfficialDate, TimeSpan.Zero, "content");
            document.InsertUnitWithoutRequireValidation(unit.Id, "file", "pdf");
        }

        private async Task<EmployeeDocumentStatus> GetEmployeeStatusAsync(Guid employeeId, CancellationToken ct)
        {
            var context = GetContext();
            var employee = await context.Employees.AsNoTracking().FirstAsync(x => x.Id == employeeId, ct);
            return employee.DocumentRepresentingStatus;
        }
    }
}
