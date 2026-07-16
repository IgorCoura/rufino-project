using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // UpdateDocumentUnitDetails é onde as policies de VENCIMENTO e CARGA HORÁRIA do template agem sobre a
    // unidade: a Validity nasce de data + IExpirationPolicy.Duration (é ela que arma o ciclo de vencimento), e o
    // WorkloadEndDate nasce da distribuição da IWorkloadPolicy em dias úteis. Estes testes fixam esse elo pelo
    // fluxo real (HTTP -> DocumentService -> Domain -> EF), com a ausência da policy como contraponto.
    //
    // Datas dinâmicas de propósito: o setter de Validity recusa validade no passado, então o cenário com
    // vencimento precisa ancorar em "hoje". O que é assertado é a RELAÇÃO (data + duração), não um valor fixo.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentUnitDetailsPolicyTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task UpdateUnitDetails_TemplateWithExpiration_SetsValidityFromThePolicy()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct,
                [new ExpirationPolicy(TimeSpan.FromDays(30))]);
            var date = DateOnly.FromDateTime(DateTime.UtcNow);

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, date));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(date, unit.Date);
            Assert.Equal(date.AddDays(30), unit.Validity);
        }

        // O contraponto: sem policy de vencimento, a Validity fica nula e o documento nunca entra no ciclo de
        // vencimento (nenhum job de expiração é armado a partir dele).
        [Fact]
        public async Task UpdateUnitDetails_TemplateWithoutExpiration_LeavesValidityNull()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct, policies: []);
            var date = DateOnly.FromDateTime(DateTime.UtcNow);

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, date));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Null(Assert.Single(result.DocumentsUnits).Validity);
        }

        // A carga horária vira WorkloadEndDate: 16h distribuídas em dias úteis a partir da data. O valor esperado
        // vem do MESMO IWorkloadCalendarService e MaxHoursWorkload do ambiente — o teste fixa a fiação
        // policy -> distribuição -> persistência, não o algoritmo do calendário (que tem testes próprios).
        [Fact]
        public async Task UpdateUnitDetails_TemplateWithWorkload_ComputesWorkloadEndDate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct,
                [new WorkloadPolicy(TimeSpan.FromHours(16))]);

            using var scope = _factory.Services.CreateScope();
            var calendar = scope.ServiceProvider.GetRequiredService<IWorkloadCalendarService>();
            var options = scope.ServiceProvider.GetRequiredService<DocumentTemplatesOptions>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var date = calendar.IsWorkingDay(today) ? today : calendar.GetNextWorkingDay(today);
            var expectedEndDate = calendar.DistributeWorkload(date, TimeSpan.FromHours(16), options.MaxHoursWorkload).EndDate;

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, date));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(expectedEndDate, unit.WorkloadEndDate);
            Assert.True(unit.WorkloadEndDate > unit.Date);
        }

        // Com carga horária, a data precisa ser dia útil — sábado é recusado com PMD.DOC19 e nada é gravado.
        [Fact]
        public async Task UpdateUnitDetails_WorkloadOnNonWorkingDay_IsRejected()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct,
                [new WorkloadPolicy(TimeSpan.FromHours(16))]);
            var saturday = NextSaturday();

            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/document/documentunit",
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, saturday));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<JsonNode>();
            Assert.Equal("PMD.DOC19", content!["errors"]!["DocumentService"]![0]!["Code"]!.ToString());

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(default, Assert.Single(result.DocumentsUnits).Date);
        }

        // O contraponto: sem policy de carga horária, a restrição de dia útil não existe (sábado passa) e nenhum
        // WorkloadEndDate é calculado.
        [Fact]
        public async Task UpdateUnitDetails_TemplateWithoutWorkload_AcceptsAnyDayAndNoEndDate()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedGeneratedDocumentAsync(context, ct, policies: []);
            var saturday = NextSaturday();

            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(seed.UnitId, seed.DocumentId, seed.EmployeeId, saturday));

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(saturday, unit.Date);
            Assert.Null(unit.WorkloadEndDate);
        }

        // Sábado nunca é dia útil (IsWorkingDay corta fim de semana antes de consultar feriados) — determinístico.
        private static DateOnly NextSaturday()
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            while (date.DayOfWeek != DayOfWeek.Saturday)
                date = date.AddDays(1);
            return date;
        }

        private sealed record GeneratedDocumentSeed(Guid CompanyId, Guid DocumentId, Guid UnitId, Guid EmployeeId);

        // Template com o conjunto EXPLÍCITO de policies informado (sem arquivo — o update pula a recuperação de
        // conteúdo), RequireDocuments por cargo e a geração real via serviço: a unidade nasce Pending, sem data.
        private async Task<GeneratedDocumentSeed> SeedGeneratedDocumentAsync(
            PeopleManagementContext context, CancellationToken ct, IEnumerable<IDocumentPolicy> policies)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Policy Doc", "Description Policy Doc", company.Id,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: false, placeSignatures: [], documentGroupId: documentGroup.Id,
                usePreviousPeriod: false,
                policies: policies);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Policy Doc", "Policy Description", [], [template.Id]);
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
            return new GeneratedDocumentSeed(company.Id, document.Id, unit.Id, employee.Id);
        }

        private async Task PutUnitDetailsAsync(Guid companyId, UpdateDocumentUnitDetailsModel command)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{companyId}/document/documentunit", command);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }
    }
}
