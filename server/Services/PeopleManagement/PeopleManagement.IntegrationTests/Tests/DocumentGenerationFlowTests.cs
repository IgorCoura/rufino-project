using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // Caracterização da decisão de "competência" HOJE — que está duplicada/implícita no fluxo:
    //   - CreateDocumentUnitsForEvent (evento recorrente) => a unidade nasce COM Period.
    //   - GenerateDocumentUnitsForRequireDocument           => a unidade nasce SEM Period.
    // Na Fase 3 a competência passa a ser definida no template (PeriodPolicy) e este comportamento muda;
    // estes testes serão reescritos e provam que a remoção da duplicação não regride a função.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentGenerationFlowTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task CreateDocumentUnitsForEvent_WithMonthlyRecurringEvent_CreatesUnitWithPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, template, employee) = await SeedActiveEmployeeAsync(context, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [employee.RoleId],
                AssociationType.Role, "Monthly Doc", "Monthly Description",
                [ListenEvent.Create(RecurringEvents.MONTHLY, [Status.Active.Id])], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.CreateDocumentUnitsForEvent(employee.Id, company.Id, RecurringEvents.MONTHLY, ct);
                await scope.ServiceProvider.GetRequiredService<IDocumentRepository>().UnitOfWork.SaveChangesAsync(ct);
            }

            var document = await GetSingleDocumentForTemplateAsync(employee.Id, template.Id, ct);
            var unit = Assert.Single(document.DocumentsUnits);
            Assert.NotNull(unit.Period);
            Assert.True(unit.IsPeriodMonthly);
        }

        [Fact]
        public async Task GenerateDocumentUnitsForRequireDocument_CreatesUnitWithoutPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, template, employee) = await SeedActiveEmployeeAsync(context, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [employee.RoleId],
                AssociationType.Role, "Role Doc", "Role Description", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(requireDocuments.Id, company.Id, ct);
            }

            var document = await GetSingleDocumentForTemplateAsync(employee.Id, template.Id, ct);
            var unit = Assert.Single(document.DocumentsUnits);
            Assert.Null(unit.Period);
        }

        // Funcionário ativo associado ao cargo, com empresa e template — mas SEM RequireDocuments ainda,
        // para que a conclusão da admissão não auto-provisione documentos (isolamento determinístico).
        private async Task<(Company Company, DocumentTemplate Template, Employee Employee)> SeedActiveEmployeeAsync(
            PeopleManagementContext context, CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            return (company, template, employee);
        }

        private async Task<Document> GetSingleDocumentForTemplateAsync(Guid employeeId, Guid templateId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            return await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.EmployeeId == employeeId && x.DocumentTemplateId == templateId, ct);
        }
    }
}
