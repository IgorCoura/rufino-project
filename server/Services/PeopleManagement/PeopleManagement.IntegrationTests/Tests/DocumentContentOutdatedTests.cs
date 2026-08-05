using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands;
using PeopleManagement.Application.Commands.DocumentCommands.CheckOutdatedDocumentContent;
using PeopleManagement.Application.Commands.DocumentCommands.MarkAsNotApplicableDocumentUnit;
using PeopleManagement.Application.Commands.DocumentCommands.RefreshDocumentContent;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using EmployeeContact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;

namespace PeopleManagement.IntegrationTests.Tests
{
    // O Content da unidade é um SNAPSHOT dos dados do funcionário, tirado no UpdateDocumentUnitDetails e consumido
    // depois pela geração do PDF. Como os dados mudam com o tempo, o snapshot envelhece em silêncio.
    //
    // Estes testes fixam a verificação desse envelhecimento: o conteúdo é remontado pelo MESMO caminho da gravação
    // (IDocumentContentBuilder) com as datas já gravadas na unidade, e confrontado com o Content cru — sem
    // normalizar nem converter nada. O primeiro teste é, na prática, a regressão de formato: se o caminho isolado
    // deixar de reproduzir a gravação byte a byte, ele acusa divergência onde nada mudou.
    //
    // O template usa APENAS RecoverDataType.Employee de propósito: tipos que falham ao recuperar (PGR sem dado,
    // por exemplo) marcam a verificação como inconclusiva, e o que se quer exercitar aqui é a comparação.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentContentOutdatedTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task CheckOutdated_WhenEmployeeDataDidNotChange_ReportsUpToDate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct);
            await PutUnitDetailsAsync(seed.CompanyId, seed.Units[0], DateOnly.FromDateTime(DateTime.UtcNow));

            var result = await PostCheckAsync(seed.CompanyId, [seed.Units[0]]);

            var item = Assert.Single(result.Items);
            Assert.Equal(seed.Units[0].DocumentUnitId, item.DocumentUnitId);
            Assert.False(item.IsOutdated);
            Assert.False(item.CheckFailed);
        }

        [Fact]
        public async Task CheckOutdated_WhenEmployeeDataChanged_ReportsOutdated()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct);
            await PutUnitDetailsAsync(seed.CompanyId, seed.Units[0], DateOnly.FromDateTime(DateTime.UtcNow));

            await ChangeEmployeeContactAsync(seed.Units[0].EmployeeId, ct);

            var result = await PostCheckAsync(seed.CompanyId, [seed.Units[0]]);

            var item = Assert.Single(result.Items);
            Assert.True(item.IsOutdated);
            Assert.False(item.CheckFailed);
        }

        // Unidade recém-gerada não tem snapshot nenhum: não há "antes" para comparar, então nada a avisar. Gerar
        // um documento assim já é barrado em outro lugar (a geração exige conteúdo).
        [Fact]
        public async Task CheckOutdated_WhenUnitHasNoContentYet_ReportsUpToDate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct);

            var result = await PostCheckAsync(seed.CompanyId, [seed.Units[0]]);

            var item = Assert.Single(result.Items);
            Assert.False(item.IsOutdated);
            Assert.False(item.CheckFailed);
        }

        // O aviso é por documento: numa seleção de dois, só o do funcionário alterado é marcado.
        [Fact]
        public async Task CheckOutdated_WithMixedBatch_FlagsOnlyTheChangedUnit()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct, employeeCount: 2);
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            await PutUnitDetailsAsync(seed.CompanyId, seed.Units[0], date);
            await PutUnitDetailsAsync(seed.CompanyId, seed.Units[1], date);

            await ChangeEmployeeContactAsync(seed.Units[1].EmployeeId, ct);

            var result = await PostCheckAsync(seed.CompanyId, [seed.Units[0], seed.Units[1]]);

            Assert.Equal(2, result.Items.Count);
            Assert.False(result.Items.Single(x => x.DocumentUnitId == seed.Units[0].DocumentUnitId).IsOutdated);
            Assert.True(result.Items.Single(x => x.DocumentUnitId == seed.Units[1].DocumentUnitId).IsOutdated);
        }

        // Renovar regrava o snapshot com os dados atuais SEM mexer na data do documento — a data é história da
        // unidade, o snapshot é o retrato dos dados.
        [Fact]
        public async Task RefreshContent_AfterEmployeeDataChanged_RewritesSnapshotAndKeepsDate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct);
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            await PutUnitDetailsAsync(seed.CompanyId, seed.Units[0], date);

            var contentBefore = await GetUnitContentAsync(seed.Units[0].DocumentId, ct);
            await ChangeEmployeeContactAsync(seed.Units[0].EmployeeId, ct);

            await PostRefreshAsync(seed.CompanyId, [seed.Units[0]]);

            var document = await GetDocumentAsync(seed.Units[0].DocumentId, ct);
            var unit = Assert.Single(document.DocumentsUnits);
            Assert.NotEqual(contentBefore, unit.Content);
            Assert.Equal(date, unit.Date);

            var result = await PostCheckAsync(seed.CompanyId, [seed.Units[0]]);
            Assert.False(Assert.Single(result.Items).IsOutdated);
        }

        // Renovar reusa o UpdateDocumentUnitDetails, que só age sobre unidade pendente. Uma unidade já resolvida
        // (aqui, não aplicável) é recusada em vez de ter o snapshot reescrito por baixo.
        [Fact]
        public async Task RefreshContent_WhenUnitIsNotPending_IsRejected()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct);
            var unitRef = seed.Units[0];

            var markClient = CreateClient();
            markClient.InputHeaders([seed.CompanyId]);
            var markResponse = await markClient.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/document/documentunit/not-applicable",
                new MarkAsNotApplicableDocumentUnitModel(unitRef.DocumentUnitId, unitRef.DocumentId, unitRef.EmployeeId));
            Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);
            var response = await client.PostAsJsonAsync($"/api/v1/{seed.CompanyId}/document/content/refresh",
                new RefreshDocumentContentModel([unitRef]));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private sealed record OutdatedSeed(Guid CompanyId, IReadOnlyList<DocumentUnitRef> Units);

        // Template COM arquivo (senão não há snapshot) e SEM policies explícitas: sem vencimento a data não
        // precisa ser futura, sem carga horária qualquer dia serve — o que isola a comparação nos dados do
        // funcionário.
        private async Task<OutdatedSeed> SeedGeneratedDocumentAsync(
            PeopleManagementContext context, CancellationToken ct, int employeeCount = 1)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Content Doc", "Description Content Doc", company.Id,
                (TimeSpan?)null, null,
                TemplateFileInfo.Create(Guid.NewGuid().ToString(), "index.html", "header.html", "footer.html",
                    [RecoverDataType.Employee]),
                acceptsSignature: false, placeSignatures: [], documentGroupId: documentGroup.Id,
                usePreviousPeriod: false,
                policies: []);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            for (var i = 0; i < employeeCount; i++)
                await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Content Doc", "Content Description", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(requireDocuments.Id, company.Id, ct);
            }

            using var readScope = _factory.Services.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var documents = await readContext.Documents.AsNoTracking().Include(x => x.DocumentsUnits)
                .Where(x => x.DocumentTemplateId == template.Id)
                .OrderBy(x => x.EmployeeId)
                .ToListAsync(ct);

            Assert.Equal(employeeCount, documents.Count);

            var units = documents
                .Select(d => new DocumentUnitRef(Assert.Single(d.DocumentsUnits).Id, d.Id, d.EmployeeId))
                .ToList();

            return new OutdatedSeed(company.Id, units);
        }

        // Contato entra no bloco Employee do snapshot, então trocá-lo é o jeito mais direto de simular "o cadastro
        // mudou depois que o documento foi preparado".
        private async Task ChangeEmployeeContactAsync(Guid employeeId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var employee = await context.Employees.FirstAsync(x => x.Id == employeeId, ct);
            employee.Contact = EmployeeContact.Create("novo-email@email.com", "(00) 100000002");
            await context.SaveChangesAsync(ct);
        }

        private async Task<string> GetUnitContentAsync(Guid documentId, CancellationToken ct)
        {
            var document = await GetDocumentAsync(documentId, ct);
            return Assert.Single(document.DocumentsUnits).Content;
        }

        private async Task PutUnitDetailsAsync(Guid companyId, DocumentUnitRef unitRef, DateOnly date)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{companyId}/document/documentunit",
                new UpdateDocumentUnitDetailsModel(unitRef.DocumentUnitId, unitRef.DocumentId, unitRef.EmployeeId, date));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }

        private async Task<CheckOutdatedDocumentContentResponse> PostCheckAsync(Guid companyId, IEnumerable<DocumentUnitRef> units)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            var response = await client.PostAsJsonAsync($"/api/v1/{companyId}/document/content/check-outdated",
                new CheckOutdatedDocumentContentModel(units));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
            return await response.Content.ReadFromJsonAsync<CheckOutdatedDocumentContentResponse>()
                ?? throw new ArgumentNullException(nameof(response));
        }

        private async Task PostRefreshAsync(Guid companyId, IEnumerable<DocumentUnitRef> units)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            var response = await client.PostAsJsonAsync($"/api/v1/{companyId}/document/content/refresh",
                new RefreshDocumentContentModel(units));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }
    }
}
