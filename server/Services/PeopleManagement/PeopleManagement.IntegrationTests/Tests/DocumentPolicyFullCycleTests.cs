using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.RenewDocumentUnit;
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
    // limitado a 1 ciclo + carga horária + competência mensal + assinatura) atravessa o ciclo inteiro — evento
    // gera a unidade, o update aplica competência/validade/carga de uma vez, o documento fica OK, vence, é
    // renovado à mão (a renovada nasce na competência mínima, sem data de referência), e aí o teto age: a
    // renovada nasce SEM validade e o documento para de vencer. Prova que as policies agem juntas sem interferir
    // umas nas outras.
    //
    // A renovação é um passo explícito porque o vencimento não cria mais nada: o job só move o status.
    //
    // O teto não recusa ação nenhuma — no fim o RH renova de novo com os ciclos esgotados e é atendido.
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
            //    do serviço) — o evento só dispara, quem decide a competência é o template (lido na hora).
            var born = await GetDocumentAsync(seed.DocumentId, ct);
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

            // 4) Primeiro vencimento: a unidade fica VENCIDA (não há substituto entregue) e nada mais acontece —
            //    o job só avisa. Renovar é decisão do RH.
            await DepreciateAsync(seed.DocumentId, bornUnit.Id, seed.CompanyId, ct);

            var afterExpiration = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Expired, Assert.Single(afterExpiration.DocumentsUnits).Status);
            Assert.Equal(0, afterExpiration.RenewalCount);

            // 5) O RH renova: ainda dentro do teto (0 renovações consumidas < 1). A substituta nasce SEM data de
            //    referência, na competência mínima, esperando a real, e vinculada à que ela substitui.
            await RenewAsync(seed.CompanyId, seed.DocumentId, seed.EmployeeId, bornUnit.Id);

            var afterFirst = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, afterFirst.DocumentsUnits.Count);
            Assert.Equal(1, afterFirst.RenewalCount);
            var renewedUnit = Assert.Single(afterFirst.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
            Assert.Equal(bornUnit.Id, renewedUnit.ReplacesDocumentUnitId);
            Assert.NotNull(renewedUnit.Period);
            Assert.Equal(Period.MIN_YEAR, renewedUnit.Period!.Year);

            // 6) A renovada recebe a data real pelo endpoint (sai da competência mínima). O teto de 1 ciclo já
            //    foi gasto na renovação, então ela nasce SEM validade: o documento parou de vencer, e é o teto
            //    agindo no único lugar em que ele age. Entregue, é o substituto que a vencida esperava, e a
            //    primeira vira histórico (Deprecated).
            await PutUnitDetailsAsync(seed.CompanyId,
                new UpdateDocumentUnitDetailsModel(renewedUnit.Id, seed.DocumentId, seed.EmployeeId, date));

            var afterDate = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Null(afterDate.DocumentsUnits.First(u => u.Id == renewedUnit.Id).Validity);

            await MakeUnitOkAsync(seed.DocumentId, renewedUnit.Id, ct);

            var afterReplacement = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Deprecated, afterReplacement.DocumentsUnits.First(u => u.Id == bornUnit.Id).Status);

            // 7) Com os ciclos esgotados o RH ainda renova: o teto governa validade, não permissão. A nova
            //    substituta é criada, vinculada, e NÃO consome ciclo (não vence, não há ciclo a consumir). O
            //    documento continua OK — a renovação em voo não conta enquanto a substituída cobre.
            var renewAtCap = await RenewAsync(seed.CompanyId, seed.DocumentId, seed.EmployeeId, renewedUnit.Id);
            Assert.Equal(HttpStatusCode.OK, renewAtCap.StatusCode);

            var final = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(3, final.DocumentsUnits.Count);
            Assert.Equal(DocumentUnitStatus.Deprecated, final.DocumentsUnits.First(u => u.Id == bornUnit.Id).Status);
            Assert.Equal(DocumentUnitStatus.OK, final.DocumentsUnits.First(u => u.Id == renewedUnit.Id).Status);
            Assert.Equal(1, final.RenewalCount);
            var atCapUnit = Assert.Single(final.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
            Assert.Equal(renewedUnit.Id, atCapUnit.ReplacesDocumentUnitId);
            Assert.Equal(DocumentStatus.OK, final.Status);
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

        private async Task MakeUnitOkAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            document.InsertUnitWithoutRequireValidation(unitId, "file", "pdf");
            await context.SaveChangesAsync(ct);
        }

        private async Task<HttpResponseMessage> RenewAsync(Guid companyId, Guid documentId, Guid employeeId, Guid unitId)
        {
            var client = CreateClient();
            client.InputHeaders([companyId]);
            return await client.PostAsJsonAsync($"/api/v1/{companyId}/document/documentunit/renew",
                new RenewDocumentUnitModel(unitId, documentId, employeeId));
        }

        private async Task DepreciateAsync(Guid documentId, Guid unitId, Guid companyId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
            await service.DepreciateExpirateDocument(unitId, documentId, companyId, ct);
        }
    }
}
