using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net;
using System.Net.Http.Json;
using static PeopleManagement.Application.Queries.DocumentDashboard.DocumentDashboardDtos;

namespace PeopleManagement.IntegrationTests.Tests
{
    // O dashboard responde "o que precisa de ação agora": vencidos (Deprecated/Invalid não substituídos +
    // OK/Warning com validade passada), a vencer (OK/Warning dentro do horizonte), pendentes, aguardando
    // assinatura e requer validação — com o MESMO predicado no summary e na lista de unidades.
    // As validades são ancoradas em "hoje" porque o setter de Validity rejeita datas passadas.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentDashboardTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

        [Fact]
        public async Task GetSummary_WithUnitsInEachState_CountsEachBucketOnce()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var scenario = await SeedCompanyWithEmployeeAsync(context, ct);

            await SeedPendingDocumentAsync(context, scenario, ct);
            await SeedOkDocumentAsync(context, scenario, validityInDays: 10, ct);
            var (expiredDoc, expiredUnitId) = await SeedOkDocumentAsync(context, scenario, validityInDays: 5, ct);
            await SeedRequiresValidationDocumentAsync(context, scenario, ct);
            await DepreciateAsync(expiredDoc.Id, expiredUnitId, scenario.CompanyId, ct);

            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var response = await client.GetAsync($"/api/v1/{scenario.CompanyId}/document-dashboard/summary");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>() ?? throw new ArgumentNullException();
            Assert.Equal(1, summary.Expired);
            Assert.Equal(1, summary.Expiring);
            // Pendente semeado + pendente de renovação criado pela depreciação.
            Assert.Equal(2, summary.Pending);
            Assert.Equal(1, summary.RequiresValidation);
            Assert.Equal(0, summary.AwaitingSignature);
        }

        [Fact]
        public async Task GetSummary_WithValidityBeyondHorizon_CountsAsExpiringOnlyWhenHorizonCoversIt()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var scenario = await SeedCompanyWithEmployeeAsync(context, ct);

            await SeedOkDocumentAsync(context, scenario, validityInDays: 40, ct);

            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var defaultHorizon = await client.GetFromJsonAsync<DashboardSummaryDto>(
                $"/api/v1/{scenario.CompanyId}/document-dashboard/summary") ?? throw new ArgumentNullException();
            var extendedHorizon = await client.GetFromJsonAsync<DashboardSummaryDto>(
                $"/api/v1/{scenario.CompanyId}/document-dashboard/summary?expiringInDays=60") ?? throw new ArgumentNullException();

            Assert.Equal(0, defaultHorizon.Expiring);
            Assert.Equal(1, extendedHorizon.Expiring);
        }

        [Fact]
        public async Task GetSummary_WhenDeprecatedUnitWasSupersededByOkUnit_DoesNotCountAsExpired()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var scenario = await SeedCompanyWithEmployeeAsync(context, ct);

            var (document, okUnitId) = await SeedOkDocumentAsync(context, scenario, validityInDays: 5, ct);
            await DepreciateAsync(document.Id, okUnitId, scenario.CompanyId, ct);
            await MakeRenewalUnitOkAsync(document.Id, validityInDays: 300, ct);

            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var summary = await client.GetFromJsonAsync<DashboardSummaryDto>(
                $"/api/v1/{scenario.CompanyId}/document-dashboard/summary") ?? throw new ArgumentNullException();

            Assert.Equal(0, summary.Expired);
            Assert.Equal(0, summary.Pending);
            Assert.Equal(0, summary.Expiring);
        }

        [Fact]
        public async Task GetUnits_ExpiringBucket_OrdersByValidityAndExposesEmployeeAndTemplateData()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var scenario = await SeedCompanyWithEmployeeAsync(context, ct);

            await SeedOkDocumentAsync(context, scenario, validityInDays: 5, ct);
            await SeedOkDocumentAsync(context, scenario, validityInDays: 2, ct);

            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var result = await client.GetFromJsonAsync<DashboardUnitsResult>(
                $"/api/v1/{scenario.CompanyId}/document-dashboard/units?bucket=Expiring") ?? throw new ArgumentNullException();

            Assert.Equal(2, result.TotalCount);
            var items = result.Items.ToList();
            Assert.Equal(Today.AddDays(2), items[0].Validity);
            Assert.Equal(Today.AddDays(5), items[1].Validity);
            Assert.Equal("ROSDEVALDO PEREIRA", items[0].EmployeeName.ToUpper());
            Assert.False(string.IsNullOrWhiteSpace(items[0].DocumentTemplateName));
            Assert.False(string.IsNullOrWhiteSpace(items[0].DocumentGroupName));
            Assert.Equal(DocumentUnitStatus.OK.Id, items[0].Status.Id);
            Assert.True(items[0].HasFile);
        }

        [Fact]
        public async Task GetUnits_WithPaginationAndTemplateFilter_LimitsItemsAndKeepsTotalCount()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var scenario = await SeedCompanyWithEmployeeAsync(context, ct);

            var (firstDoc, _) = await SeedOkDocumentAsync(context, scenario, validityInDays: 5, ct);
            await SeedOkDocumentAsync(context, scenario, validityInDays: 2, ct);

            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var paged = await client.GetFromJsonAsync<DashboardUnitsResult>(
                $"/api/v1/{scenario.CompanyId}/document-dashboard/units?bucket=Expiring&pageSize=1&pageNumber=1")
                ?? throw new ArgumentNullException();
            var filtered = await client.GetFromJsonAsync<DashboardUnitsResult>(
                $"/api/v1/{scenario.CompanyId}/document-dashboard/units?bucket=Expiring&documentTemplateId={firstDoc.DocumentTemplateId}")
                ?? throw new ArgumentNullException();

            Assert.Equal(2, paged.TotalCount);
            Assert.Single(paged.Items);
            Assert.Equal(1, filtered.TotalCount);
            Assert.Equal(firstDoc.Id, filtered.Items.Single().DocumentId);
        }

        [Fact]
        public async Task GetSummary_WhenRouteCompanyIsNotInToken_ReturnsForbidden()
        {
            var ct = CancellationToken.None;
            var context = GetContext();
            var scenario = await SeedCompanyWithEmployeeAsync(context, ct);
            var otherCompany = Guid.NewGuid();

            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var response = await client.GetAsync($"/api/v1/{otherCompany}/document-dashboard/summary");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private sealed record DashboardScenario(Guid CompanyId, Guid RoleId, Guid EmployeeId);

        private static async Task<DashboardScenario> SeedCompanyWithEmployeeAsync(
            PeopleManagementContext context, CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);
            return new DashboardScenario(company.Id, role.Id, employee.Id);
        }

        // Cria template + require-documents + documento com uma unidade Pending recém-nascida.
        private static async Task<(Document Document, Guid UnitId)> SeedPendingDocumentAsync(
            PeopleManagementContext context, DashboardScenario scenario, CancellationToken ct)
        {
            var (document, unit) = await SeedDocumentWithUnitAsync(context, scenario, ct);
            await context.SaveChangesAsync(ct);
            return (document, unit.Id);
        }

        // Unidade OK com validade em Today + validityInDays (o setter de Validity exige data futura).
        private static async Task<(Document Document, Guid UnitId)> SeedOkDocumentAsync(
            PeopleManagementContext context, DashboardScenario scenario, int validityInDays, CancellationToken ct)
        {
            var (document, unit) = await SeedDocumentWithUnitAsync(context, scenario, ct);
            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.FromDays(validityInDays), "content");
            document.InsertUnitWithoutRequireValidation(unit.Id, "file", "pdf");
            await context.SaveChangesAsync(ct);
            return (document, unit.Id);
        }

        private static async Task<(Document Document, Guid UnitId)> SeedRequiresValidationDocumentAsync(
            PeopleManagementContext context, DashboardScenario scenario, CancellationToken ct)
        {
            var (document, unit) = await SeedDocumentWithUnitAsync(context, scenario, ct);
            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.FromDays(200), "content");
            document.InsertUnitWithRequireValidation(unit.Id, "file", "pdf");
            await context.SaveChangesAsync(ct);
            return (document, unit.Id);
        }

        private static async Task<(Document Document, DocumentUnit Unit)> SeedDocumentWithUnitAsync(
            PeopleManagementContext context, DashboardScenario scenario, CancellationToken ct)
        {
            var template = await context.InsertDocumentTemplate(scenario.CompanyId, ct);
            await context.SaveChangesAsync(ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), scenario.CompanyId, [scenario.RoleId],
                AssociationType.Role, "Doc Role Required", "Description Doc Role Required", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);

            var document = Document.Create(Guid.NewGuid(), scenario.EmployeeId, scenario.CompanyId,
                requireDocuments.Id, template.Id, template.Name.ToString(), template.Description.ToString());
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            await context.Documents.AddAsync(document, ct);

            return (document, unit);
        }

        private async Task DepreciateAsync(Guid documentId, Guid unitId, Guid companyId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
            await service.DepreciateExpirateDocument(unitId, documentId, companyId, ct);
        }

        // Valida a unidade Pending criada pela renovação, tornando-a a unidade vigente do documento.
        private async Task MakeRenewalUnitOkAsync(Guid documentId, int validityInDays, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.Id == documentId, ct);
            var pendingUnit = document.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending);
            document.UpdateDocumentUnitDetails(pendingUnit.Id, Today, TimeSpan.FromDays(validityInDays), "content");
            document.InsertUnitWithoutRequireValidation(pendingUnit.Id, "file", "pdf");
            await context.SaveChangesAsync(ct);
        }
    }
}
