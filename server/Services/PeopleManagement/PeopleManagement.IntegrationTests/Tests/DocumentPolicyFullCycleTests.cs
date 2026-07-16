using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // O teste-espinha-dorsal do Composite: um template com as QUATRO policies ao mesmo tempo (vencimento
    // limitado a 1 renovação + carga horária + competência mensal + assinatura) atravessa o ciclo inteiro —
    // evento gera a unidade, o update aplica competência/validade/carga de uma vez, o documento fica OK, vence e
    // renova (a renovada nasce na competência mínima, sem data de referência), vence de novo e PARA no teto.
    // Prova que as policies agem juntas sem interferir umas nas outras.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentPolicyFullCycleTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task FullLifecycle_TemplateWithAllPolicies_BehavesCorrectlyAtEveryStep()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedAllPoliciesDocumentByEventAsync(context, ct);

            // 1) Nascimento pelo evento: a unidade nasce Pending já situada numa competência mensal (a do "agora"
            //    do serviço) — o evento só dispara, quem decide a competência é o template.
            var born = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(PeriodType.Monthly, born.PeriodType);
            var bornUnit = Assert.Single(born.DocumentsUnits);
            Assert.NotNull(bornUnit.Period);
            Assert.True(bornUnit.Period!.IsMonthly);

            // 2) Update com data: as três policies de dados agem de uma vez — competência recomputada da data,
            //    Validity = data + 365d, WorkloadEndDate calculado em dias úteis.
            //    Data dinâmica: a Validity não pode cair no passado, então ancora em "hoje" (dia útil).
            var date = await NextWorkingDayAsync(DateOnly.FromDateTime(DateTime.UtcNow));
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(bornUnit.Id, seed.DocumentId, seed.EmployeeId, date));

            var updated = await GetDocumentAsync(seed.DocumentId, ct);
            var updatedUnit = Assert.Single(updated.DocumentsUnits);
            Assert.Equal(date.Year, updatedUnit.Period!.Year);
            Assert.Equal(date.Month, updatedUnit.Period.Month);
            Assert.Equal(date.AddDays(365), updatedUnit.Validity);
            Assert.NotNull(updatedUnit.WorkloadEndDate);

            // 3) Documento entregue e OK.
            await MakeUnitOkAsync(seed.DocumentId, bornUnit.Id, ct);

            // 4) Primeiro vencimento: ainda dentro do teto (0 renovações consumidas < 1) — deprecia a vencida e
            //    RENOVA. A unidade renovada nasce SEM data de referência, na competência mínima, esperando a real.
            await DepreciateAsync(seed.DocumentId, bornUnit.Id, seed.CompanyId, ct);

            var afterFirst = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, afterFirst.DocumentsUnits.Count);
            Assert.Equal(DocumentUnitStatus.Deprecated, afterFirst.DocumentsUnits.First(u => u.Id == bornUnit.Id).Status);
            var renewedUnit = Assert.Single(afterFirst.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
            Assert.NotNull(renewedUnit.Period);
            Assert.Equal(Period.MIN_YEAR, renewedUnit.Period!.Year);

            // 5) A renovada recebe a data real (sai da competência mínima), é entregue e vence: o teto
            //    (1 renovação) foi atingido — deprecia e NÃO cria outra.
            await SetUnitDateAsync(seed.DocumentId, renewedUnit.Id, date, ct);
            await MakeUnitOkAsync(seed.DocumentId, renewedUnit.Id, ct);
            await DepreciateAsync(seed.DocumentId, renewedUnit.Id, seed.CompanyId, ct);

            var final = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, final.DocumentsUnits.Count);
            Assert.Equal(2, final.DocumentsUnits.Count(u => u.Status == DocumentUnitStatus.Deprecated));
            Assert.DoesNotContain(final.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
        }

        private sealed record AllPoliciesSeed(Guid CompanyId, Guid DocumentId, Guid EmployeeId);

        // Template com o conjunto completo: vencimento limitado (365d, 1 renovação) + carga (8h) + competência
        // mensal + assinatura (1 local). Sem arquivo — o update pula a recuperação de conteúdo. O documento é
        // gerado pelo EVENTO recorrente mensal, o gatilho real do fluxo por competência.
        private async Task<AllPoliciesSeed> SeedAllPoliciesDocumentByEventAsync(
            PeopleManagementContext context, CancellationToken ct)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "All Policies", "Description All Policies", company.Id,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: true,
                placeSignatures: [PlaceSignature.Create(TypeSignature.Signature, 1, 10, 10, 20, 10)],
                documentGroupId: documentGroup.Id,
                usePreviousPeriod: false,
                policies:
                [
                    new ExpirationLimitedPolicy(TimeSpan.FromDays(365), maxRenewals: 1),
                    new WorkloadPolicy(TimeSpan.FromHours(8)),
                    new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod: false),
                ]);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "All Policies Doc", "All Policies Description",
                [ListenEvent.Create(RecurringEvents.MONTHLY, [Status.Active.Id])], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.CreateDocumentUnitsForEvent(employee.Id, company.Id, RecurringEvents.MONTHLY, ct);
                await scope.ServiceProvider.GetRequiredService<IDocumentRepository>().UnitOfWork.SaveChangesAsync(ct);
            }

            using var readScope = _factory.Services.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await readContext.Documents.AsNoTracking()
                .FirstAsync(x => x.EmployeeId == employee.Id && x.DocumentTemplateId == template.Id, ct);
            return new AllPoliciesSeed(company.Id, document.Id, employee.Id);
        }

        private async Task<DateOnly> NextWorkingDayAsync(DateOnly date)
        {
            using var scope = _factory.Services.CreateScope();
            var calendar = scope.ServiceProvider
                .GetRequiredService<Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar.IWorkloadCalendarService>();
            return calendar.IsWorkingDay(date) ? date : calendar.GetNextWorkingDay(date);
        }

        private async Task PutUnitDetailsAsync(Guid companyId, UpdateDocumentUnitDetailsModel command)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{companyId}/document/documentunit", command);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }

        // Dá a data real à unidade (a renovada nasce sem data, na competência mínima) — pré-requisito para
        // entregá-la, pois InsertUnitWithoutRequireValidation recusa unidade sem data válida.
        private async Task SetUnitDateAsync(Guid documentId, Guid unitId, DateOnly date, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            document.UpdateDocumentUnitDetails(unitId, date, TimeSpan.Zero, "");
            await context.SaveChangesAsync(ct);
        }

        private async Task MakeUnitOkAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            document.InsertUnitWithoutRequireValidation(unitId, "file", "pdf");
            await context.SaveChangesAsync(ct);
        }

        private async Task DepreciateAsync(Guid documentId, Guid unitId, Guid companyId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
            await service.DepreciateExpirateDocument(unitId, documentId, companyId, ct);
        }
    }
}
