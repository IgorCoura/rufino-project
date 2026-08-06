using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.Net.Http.Json;
using static PeopleManagement.Application.Queries.BatchDocument.BatchDocumentDtos;

namespace PeopleManagement.IntegrationTests.Tests
{
    // Grupo, template e funcionário são três filtros independentes: nenhum é obrigatório e qualquer
    // combinação vale. O template saiu da rota justamente para permitir "tudo que falta do Fulano",
    // que atravessa grupos e templates.
    [Collection(nameof(IntegrationTestCollection))]
    public class BatchDocumentTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

        [Fact]
        public async Task GetPendingUnits_WithoutAnyFilter_ReturnsEveryPendingUnitOfTheCompany()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetPendingUnitsAsync(scenario, string.Empty);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count());
        }

        [Fact]
        public async Task GetPendingUnits_FilteredByEmployeeOnly_ReturnsUnitsAcrossGroupsAndTemplates()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetPendingUnitsAsync(scenario, $"employeeId={scenario.FirstEmployeeId}");

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, item => Assert.Equal(scenario.FirstEmployeeId, item.EmployeeId));
            Assert.Contains(result.Items, i => i.DocumentTemplateId == scenario.GroupedTemplateId);
            Assert.Contains(result.Items, i => i.DocumentTemplateId == scenario.OtherGroupTemplateId);
        }

        [Fact]
        public async Task GetPendingUnits_FilteredByGroupOnly_ReturnsEveryTemplateOfThatGroup()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetPendingUnitsAsync(scenario, $"documentGroupId={scenario.GroupId}");

            Assert.Equal(2, result.TotalCount);
            Assert.DoesNotContain(result.Items, i => i.DocumentTemplateId == scenario.OtherGroupTemplateId);
        }

        [Fact]
        public async Task GetPendingUnits_FilteredByGroupAndTemplate_NarrowsDownToTheTemplate()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetPendingUnitsAsync(
                scenario, $"documentGroupId={scenario.GroupId}&documentTemplateId={scenario.GroupedTemplateId}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(scenario.GroupedTemplateId, result.Items.Single().DocumentTemplateId);
            Assert.Equal(scenario.FirstEmployeeId, result.Items.Single().EmployeeId);
        }

        [Fact]
        public async Task GetPendingUnits_FilteredByEmployeeAndGroup_CombinesBothAxes()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetPendingUnitsAsync(
                scenario, $"employeeId={scenario.SecondEmployeeId}&documentGroupId={scenario.GroupId}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(scenario.SecondGroupedTemplateId, result.Items.Single().DocumentTemplateId);
        }

        [Fact]
        public async Task GetPendingUnits_WithMixedTemplates_ExposesTemplateAndGroupIdentity()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetPendingUnitsAsync(scenario, string.Empty);

            Assert.All(result.Items, item =>
            {
                Assert.NotEqual(Guid.Empty, item.DocumentTemplateId);
                Assert.False(string.IsNullOrWhiteSpace(item.DocumentTemplateName));
                Assert.False(string.IsNullOrWhiteSpace(item.DocumentGroupName));
            });
        }

        [Fact]
        public async Task GetPendingUnits_PagedOneByOne_KeepsTotalCountAndNeverRepeatsAUnit()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var seen = new List<Guid>();
            for (var page = 1; page <= 3; page++)
            {
                var result = await GetPendingUnitsAsync(scenario, $"pageSize=1&pageNumber={page}");
                Assert.Equal(3, result.TotalCount);
                seen.Add(result.Items.Single().DocumentUnitId);
            }

            Assert.Equal(3, seen.Distinct().Count());
        }

        [Fact]
        public async Task GetPendingUnits_FilteredByEmployeeStatus_StillNarrowsTheResult()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var active = await GetPendingUnitsAsync(scenario, "employeeStatusId=2");
            var inactive = await GetPendingUnitsAsync(scenario, "employeeStatusId=5");

            Assert.Equal(3, active.TotalCount);
            Assert.Equal(0, inactive.TotalCount);
        }

        [Fact]
        public async Task GetMissingEmployees_WithoutGroupOrTemplate_ReturnsEmpty()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);

            var result = await GetMissingEmployeesAsync(scenario, string.Empty);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMissingEmployees_ByGroup_ReturnsOneRowPerTemplateWithoutPendingUnit()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);
            // Documento entregue: existe, mas sem unidade pendente — é o que falta gerar.
            await SeedDeliveredDocumentAsync(scenario, scenario.SecondEmployeeId, scenario.GroupedTemplateId, ct);

            var result = await GetMissingEmployeesAsync(scenario, $"documentGroupId={scenario.GroupId}");

            var row = Assert.Single(result);
            Assert.Equal(scenario.SecondEmployeeId, row.EmployeeId);
            Assert.Equal(scenario.GroupedTemplateId, row.DocumentTemplateId);
            Assert.False(string.IsNullOrWhiteSpace(row.DocumentTemplateName));
        }

        [Fact]
        public async Task GetMissingEmployees_ByTemplate_IgnoresOtherTemplatesOfTheSameGroup()
        {
            var ct = CancellationToken.None;
            var scenario = await SeedScenarioAsync(ct);
            await SeedDeliveredDocumentAsync(scenario, scenario.SecondEmployeeId, scenario.GroupedTemplateId, ct);
            await SeedDeliveredDocumentAsync(scenario, scenario.FirstEmployeeId, scenario.SecondGroupedTemplateId, ct);

            var result = await GetMissingEmployeesAsync(
                scenario, $"documentTemplateId={scenario.GroupedTemplateId}");

            var row = Assert.Single(result);
            Assert.Equal(scenario.GroupedTemplateId, row.DocumentTemplateId);
        }

        private async Task<BatchDocumentUnitsResult> GetPendingUnitsAsync(BatchScenario scenario, string query)
        {
            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var suffix = string.IsNullOrEmpty(query) ? string.Empty : $"?{query}";
            return await client.GetFromJsonAsync<BatchDocumentUnitsResult>(
                $"/api/v1/{scenario.CompanyId}/batch-document/pending-units{suffix}")
                ?? throw new ArgumentNullException();
        }

        private async Task<List<EmployeeMissingDocumentDto>> GetMissingEmployeesAsync(BatchScenario scenario, string query)
        {
            var client = CreateClient();
            client.InputHeaders([scenario.CompanyId]);
            var suffix = string.IsNullOrEmpty(query) ? string.Empty : $"?{query}";
            return await client.GetFromJsonAsync<List<EmployeeMissingDocumentDto>>(
                $"/api/v1/{scenario.CompanyId}/batch-document/missing-employees{suffix}")
                ?? throw new ArgumentNullException();
        }

        private sealed record BatchScenario(
            Guid CompanyId,
            Guid RequireDocumentsId,
            Guid FirstEmployeeId,
            Guid SecondEmployeeId,
            Guid GroupId,
            Guid GroupedTemplateId,
            Guid SecondGroupedTemplateId,
            Guid OtherGroupTemplateId);

        // Dois funcionários, dois grupos, três templates e três unidades pendentes:
        // funcionário 1 tem pendência nos dois grupos, funcionário 2 só no primeiro.
        private async Task<BatchScenario> SeedScenarioAsync(CancellationToken ct)
        {
            var context = GetContext();

            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            await context.SaveChangesAsync(ct);

            // Funcionários antes do RequireDocuments: a admissão dispara geração de
            // documentos e criaria pendências fora do controle do cenário.
            var firstEmployee = await context.InsertEmployeeActive(company.Id, role.Id, ct);
            var secondEmployee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var group = await context.InsertDocumentGroup(company.Id, ct);
            var otherGroup = await context.InsertDocumentGroup(company.Id, ct);
            var groupedTemplate = CreateTemplate(context, company.Id, group.Id, "Template Grupo A1");
            var secondGroupedTemplate = CreateTemplate(context, company.Id, group.Id, "Template Grupo A2");
            var otherGroupTemplate = CreateTemplate(context, company.Id, otherGroup.Id, "Template Grupo B1");
            await context.SaveChangesAsync(ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Doc Role Required", "Description Doc Role Required", [],
                [groupedTemplate.Id, secondGroupedTemplate.Id, otherGroupTemplate.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            var scenario = new BatchScenario(company.Id, requireDocuments.Id, firstEmployee.Id, secondEmployee.Id,
                group.Id, groupedTemplate.Id, secondGroupedTemplate.Id, otherGroupTemplate.Id);

            await SeedPendingDocumentAsync(scenario, firstEmployee.Id, groupedTemplate, ct);
            await SeedPendingDocumentAsync(scenario, firstEmployee.Id, otherGroupTemplate, ct);
            await SeedPendingDocumentAsync(scenario, secondEmployee.Id, secondGroupedTemplate, ct);

            return scenario;
        }

        private static DocumentTemplate CreateTemplate(
            PeopleManagementContext context, Guid companyId, Guid groupId, string name)
        {
            var template = DocumentTemplate.Create(
                Guid.NewGuid(),
                name,
                $"Description {name}",
                companyId,
                TimeSpan.FromDays(365),
                TimeSpan.FromHours(8),
                TemplateFileInfo.Create(Guid.NewGuid().ToString(), "index.html", "header.html", "footer.html",
                    [RecoverDataType.Employee]),
                true,
                [],
                groupId);
            context.DocumentTemplates.Add(template);
            return template;
        }

        private async Task SeedPendingDocumentAsync(
            BatchScenario scenario, Guid employeeId, DocumentTemplate template, CancellationToken ct)
        {
            var context = GetContext();
            var document = Document.Create(Guid.NewGuid(), employeeId, scenario.CompanyId,
                scenario.RequireDocumentsId, template.Id, template.Name.ToString(), template.Description.ToString());
            document.NewDocumentUnit(Guid.NewGuid());
            await context.Documents.AddAsync(document, ct);
            await context.SaveChangesAsync(ct);
        }

        // Documento com a unidade já entregue (OK): some da lista de pendentes e passa a
        // ser uma pendência a criar na tela de lote.
        private async Task SeedDeliveredDocumentAsync(
            BatchScenario scenario, Guid employeeId, Guid templateId, CancellationToken ct)
        {
            var context = GetContext();
            var document = Document.Create(Guid.NewGuid(), employeeId, scenario.CompanyId,
                scenario.RequireDocumentsId, templateId, "Delivered", "Delivered document");
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, Today, TimeSpan.FromDays(365), "content");
            document.InsertUnitWithoutRequireValidation(unit.Id, "file", "pdf");
            await context.Documents.AddAsync(document, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
