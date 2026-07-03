using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // Caracterização do "vence sempre": para um documento com associação vigente, DepreciateExpirateDocument
    // deprecia a unidade vencida E cria uma nova unidade Pending, reiniciando o ciclo indefinidamente.
    // Fixa também a mecânica de contagem (unidades Deprecated acumulam), que a Fase 3 usará como contador de
    // renovações (decisão: contar units) para a policy de "vence N vezes".
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentExpirationRenewalTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly OfficialDate = new(2024, 1, 15);

        [Fact]
        public async Task DepreciateExpirate_WhenAssociated_DeprecatesUnitAndCreatesNewPendingUnit()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (companyId, document, okUnitId) = await SeedDocumentWithOkUnitAsync(context, ct);

            await DepreciateAsync(document.Id, okUnitId, companyId, ct);

            var result = await GetDocumentAsync(document.Id, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);
            Assert.Equal(DocumentUnitStatus.Deprecated, result.DocumentsUnits.First(u => u.Id == okUnitId).Status);
            Assert.Single(result.DocumentsUnits.Where(u => u.Status == DocumentUnitStatus.Pending));
        }

        [Fact]
        public async Task DepreciateExpirate_WhenAssociatedTwice_AccumulatesDeprecatedUnits()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (companyId, document, firstUnitId) = await SeedDocumentWithOkUnitAsync(context, ct);

            await DepreciateAsync(document.Id, firstUnitId, companyId, ct);

            var afterFirst = await GetDocumentAsync(document.Id, ct);
            var secondUnitId = afterFirst.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending).Id;
            await MakeUnitOkAsync(document.Id, secondUnitId, ct);

            await DepreciateAsync(document.Id, secondUnitId, companyId, ct);

            var result = await GetDocumentAsync(document.Id, ct);
            Assert.Equal(3, result.DocumentsUnits.Count);
            Assert.Equal(2, result.DocumentsUnits.Count(u => u.Status == DocumentUnitStatus.Deprecated));
            Assert.Single(result.DocumentsUnits.Where(u => u.Status == DocumentUnitStatus.Pending));
        }

        // Semeia empresa/cargo/template + funcionário ativo associado ao cargo, um RequireDocuments associado
        // ao cargo, e um Document com uma unidade OK (elegível a vencer/renovar).
        private async Task<(Guid CompanyId, Document Document, Guid OkUnitId)> SeedDocumentWithOkUnitAsync(
            PeopleManagementContext context, CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var template = await context.InsertDocumentTemplate(company.Id, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Doc Role Required", "Description Doc Role Required", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);

            var document = Document.Create(Guid.NewGuid(), employee.Id, company.Id, requireDocuments.Id, template.Id,
                template.Name.ToString(), template.Description.ToString());
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            document.UpdateDocumentUnitDetails(unit.Id, OfficialDate, TimeSpan.Zero, "content");
            document.InsertUnitWithoutRequireValidation(unit.Id, "file", "pdf");
            await context.Documents.AddAsync(document, ct);
            await context.SaveChangesAsync(ct);

            return (company.Id, document, unit.Id);
        }

        private async Task DepreciateAsync(Guid documentId, Guid unitId, Guid companyId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
            await service.DepreciateExpirateDocument(unitId, documentId, companyId, ct);
        }

        private async Task MakeUnitOkAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            document.UpdateDocumentUnitDetails(unitId, OfficialDate, TimeSpan.Zero, "content");
            document.InsertUnitWithoutRequireValidation(unitId, "file", "pdf");
            await context.SaveChangesAsync(ct);
        }
    }
}
