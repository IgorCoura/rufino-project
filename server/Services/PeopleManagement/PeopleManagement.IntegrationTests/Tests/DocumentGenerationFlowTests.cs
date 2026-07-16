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
    // A competência deixou de ser decidida pelo evento e passou a ser configuração do template (PeriodPolicy),
    // copiada e congelada no Document ao nascer. Estes testes fixam a inversão:
    //   - template COM period  => unidade nasce naquela competência, e a frequência do evento é irrelevante;
    //   - template SEM period   => unidade sem competência, mesmo disparada por evento recorrente;
    //   - fluxo requireDoc (sem data) num template por competência => competência mínima.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentGenerationFlowTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task CreateDocumentUnitsForEvent_WhenTemplateHasPeriod_CreatesUnitWithThatPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, template, employee) = await SeedActiveEmployeeAsync(context, ct, PeriodType.Monthly);

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

        // A frequência do evento não decide mais a competência: um evento recorrente num template sem period
        // gera unidade sem competência. É o coração da inversão desta fase.
        [Fact]
        public async Task CreateDocumentUnitsForEvent_WhenTemplateHasNoPeriod_CreatesUnitWithoutPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, template, employee) = await SeedActiveEmployeeAsync(context, ct, periodType: null);

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
            Assert.Null(unit.Period);
        }

        [Fact]
        public async Task GenerateDocumentUnitsForRequireDocument_WhenTemplateHasNoPeriod_CreatesUnitWithoutPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, template, employee) = await SeedActiveEmployeeAsync(context, ct, periodType: null);

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

        // Sem data de evento, mas o template é por competência: a unidade nasce na competência mínima, que uma
        // data real substitui depois. Prova que "sem data => competência mínima" vale ponta a ponta.
        [Fact]
        public async Task GenerateDocumentUnitsForRequireDocument_WhenTemplateHasPeriod_CreatesUnitAtMinimumPeriod()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (company, template, employee) = await SeedActiveEmployeeAsync(context, ct, PeriodType.Monthly);

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
            Assert.NotNull(unit.Period);
            Assert.True(unit.IsPeriodMonthly);
            Assert.Equal(Period.MIN_YEAR, unit.Period!.Year);
        }

        // Funcionário ativo associado ao cargo, com empresa e template — mas SEM RequireDocuments ainda,
        // para que a conclusão da admissão não auto-provisione documentos (isolamento determinístico).
        // periodType define se o template é por competência.
        private async Task<(Company Company, DocumentTemplate Template, Employee Employee)> SeedActiveEmployeeAsync(
            PeopleManagementContext context, CancellationToken ct, PeriodType? periodType)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct, periodType: periodType);
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
