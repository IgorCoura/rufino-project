using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // A SignaturePolicy sustenta o guard de envio para assinatura: template sem a policy => IsSignable false =>
    // o envio é recusado ANTES de qualquer chamada externa (ZapSign) e nada muda de estado. O caminho feliz do
    // envio depende da API externa e fica fora desta suíte (testes correspondentes estão com Skip); o guard, não.
    [Collection(nameof(IntegrationTestCollection))]
    public class SignDocumentPolicyGuardTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task InsertDocumentToSign_TemplateWithoutSignaturePolicy_IsRejectedBeforeAnyExternalCall()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var (companyId, documentId, unitId, employeeId) = await SeedUnsignableDocumentAsync(context, ct);

            using var scope = _factory.Services.CreateScope();
            var signService = scope.ServiceProvider.GetRequiredService<ISignDocumentService>();
            using var stream = new MemoryStream([1, 2, 3]);

            var exception = await Assert.ThrowsAsync<DomainException>(() => signService.InsertDocumentToSign(
                unitId, documentId, employeeId, companyId, "pdf", stream,
                DateTime.UtcNow.AddDays(5), 1, ct));

            Assert.Contains(exception.Errors.SelectMany(e => e.Value).Cast<Error>(),
                e => e.Code == "PMD.DOC17");

            // Nada mudou: a unidade segue Pending (não foi marcada como aguardando assinatura).
            var document = await GetDocumentAsync(documentId, ct);
            Assert.True(document.IsPendingDocumentUnit(unitId));
        }

        // Template que NÃO aceita assinatura, documento gerado pelo fluxo real com uma unidade Pending.
        private async Task<(Guid CompanyId, Guid DocumentId, Guid UnitId, Guid EmployeeId)> SeedUnsignableDocumentAsync(
            PeopleManagementContext context, CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Unsignable", "Description Unsignable", company.Id,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: false, placeSignatures: [], documentGroupId: documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Unsignable Doc", "Unsignable Description", [], [template.Id]);
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
            return (company.Id, document.Id, Assert.Single(document.DocumentsUnits).Id, employee.Id);
        }
    }
}
