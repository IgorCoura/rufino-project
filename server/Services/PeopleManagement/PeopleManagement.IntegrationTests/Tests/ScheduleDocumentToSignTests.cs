using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.CancelScheduledDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.ScheduleDocumentToSign;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;

namespace PeopleManagement.IntegrationTests.Tests
{
    /// <summary>
    /// Agendamento do envio para assinatura de ponta a ponta: agendar e cancelar pelos endpoints, a data
    /// sugerida no GET, e os caminhos em que o disparo desiste.
    ///
    /// O disparo bem-sucedido abre sessão na ZapSign, então fica de fora — mesmo motivo dos Skip em
    /// DocumentTests e do recorte do SignDocumentPolicyGuardTests. O que dá para provar aqui sem API externa é
    /// justamente o que a feature acrescenta: o agendamento persistido e as guardas do disparo.
    /// </summary>
    [Collection(nameof(IntegrationTestCollection))]
    public class ScheduleDocumentToSignTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
        private static readonly DateOnly SendOn = Today.AddDays(30);
        private static readonly DateOnly DateLimitToSign = Today.AddDays(35);

        [Fact]
        public async Task ScheduleToSign_WithPendingUnit_ShouldPersistTheSchedule()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);

            var response = await ScheduleAsync(seed, SendOn, DateLimitToSign);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var unit = await GetUnitAsync(seed, ct);
            Assert.NotNull(unit.ScheduledSignature);
            Assert.Equal(SendOn, unit.ScheduledSignature!.SendOn);
            Assert.Equal(DateLimitToSign, unit.ScheduledSignature.DateLimitToSign);
        }

        // O agendamento é intenção, não envio: a unidade continua pendente até a data chegar.
        [Fact]
        public async Task ScheduleToSign_WithPendingUnit_ShouldLeaveTheUnitPending()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);

            await ScheduleAsync(seed, SendOn, DateLimitToSign);

            Assert.Equal(DocumentUnitStatus.Pending, (await GetUnitAsync(seed, ct)).Status);
        }

        [Fact]
        public async Task ScheduleToSign_WithDateLimitBeforeSendDate_ShouldBeRejected()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);

            var response = await ScheduleAsync(seed, SendOn, SendOn.AddDays(-1));

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Null((await GetUnitAsync(seed, ct)).ScheduledSignature);
        }

        [Fact]
        public async Task ScheduleToSign_WithSendDateInThePast_ShouldBeRejected()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);

            var response = await ScheduleAsync(seed, Today.AddDays(-1), DateLimitToSign);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Null((await GetUnitAsync(seed, ct)).ScheduledSignature);
        }

        [Fact]
        public async Task CancelScheduledToSign_WithSchedule_ShouldClearIt()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);
            await ScheduleAsync(seed, SendOn, DateLimitToSign);

            var response = await CancelAsync(seed);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null((await GetUnitAsync(seed, ct)).ScheduledSignature);
        }

        [Fact]
        public async Task CancelScheduledToSign_WithoutSchedule_ShouldSucceedAnyway()
        {
            var seed = await SeedSignableDocumentAsync(CancellationToken.None);

            var response = await CancelAsync(seed);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // A sugestão é o vencimento da cobertura vigente — a unidade OK que o substituto vai render.
        [Fact]
        public async Task GetById_WithDeliveredUnit_ShouldSuggestItsValidityAsTheScheduleDate()
        {
            var validity = Today.AddDays(60);
            var seed = await SeedSignableDocumentAsync(CancellationToken.None, deliveredValidity: validity);

            var document = await GetDocumentDtoAsync(seed);

            Assert.Equal(validity, document.SuggestedSignatureScheduleDate);
        }

        // A sugestão é calculada antes do filtro de status: filtrar a lista por "Pendente" tira a unidade OK da
        // consulta, e sem esse cuidado a sugestão sumiria da tela sem motivo.
        [Fact]
        public async Task GetById_FilteringUnitsByPendingStatus_ShouldStillSuggestTheScheduleDate()
        {
            var validity = Today.AddDays(60);
            var seed = await SeedSignableDocumentAsync(CancellationToken.None, deliveredValidity: validity);

            var document = await GetDocumentDtoAsync(seed, statusId: DocumentUnitStatus.Pending.Id);

            Assert.Equal(validity, document.SuggestedSignatureScheduleDate);
            Assert.DoesNotContain(document.DocumentsUnits, u => u.Status.Id == DocumentUnitStatus.OK.Id);
        }

        [Fact]
        public async Task GetById_WithoutDeliveredUnit_ShouldNotSuggestAnyScheduleDate()
        {
            var seed = await SeedSignableDocumentAsync(CancellationToken.None);

            var document = await GetDocumentDtoAsync(seed);

            Assert.Null(document.SuggestedSignatureScheduleDate);
        }

        [Fact]
        public async Task SendScheduledDocumentToSign_AfterCancellation_ShouldDoNothing()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);
            await ScheduleAsync(seed, SendOn, DateLimitToSign);
            await CancelAsync(seed);

            await FireScheduledSendAsync(seed, SendOn, ct);

            Assert.Equal(DocumentUnitStatus.Pending, (await GetUnitAsync(seed, ct)).Status);
        }

        // O disparo antigo continua na fila do Hangfire depois de um reagendamento. Ele desiste ao ver que a
        // data que carrega não é mais a gravada — é assim que o reagendamento invalida o job anterior.
        [Fact]
        public async Task SendScheduledDocumentToSign_WithStaleSendDate_ShouldDoNothing()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);
            await ScheduleAsync(seed, SendOn, DateLimitToSign);

            var rescheduledTo = SendOn.AddDays(10);
            await ScheduleAsync(seed, rescheduledTo, rescheduledTo.AddDays(5));

            await FireScheduledSendAsync(seed, SendOn, ct);

            var unit = await GetUnitAsync(seed, ct);
            Assert.Equal(DocumentUnitStatus.Pending, unit.Status);
            Assert.Equal(rescheduledTo, unit.ScheduledSignature!.SendOn);
        }

        // Unidade que saiu de Pending perdeu o objeto do agendamento: o disparo limpa o VO para a UI parar de
        // anunciar um envio que não vai mais acontecer.
        [Fact]
        public async Task SendScheduledDocumentToSign_WhenUnitIsNoLongerPending_ShouldDropTheSchedule()
        {
            var ct = CancellationToken.None;
            var seed = await SeedSignableDocumentAsync(ct);
            await ScheduleAsync(seed, SendOn, DateLimitToSign);
            await DeliverUnitAsync(seed, ct);

            await FireScheduledSendAsync(seed, SendOn, ct);

            var unit = await GetUnitAsync(seed, ct);
            Assert.Equal(DocumentUnitStatus.OK, unit.Status);
            Assert.Null(unit.ScheduledSignature);
        }

        private sealed record ScheduleSeed(Guid CompanyId, Guid EmployeeId, Guid DocumentId, Guid DocumentUnitId);

        /// <summary>
        /// Template assinável sem arquivo, documento gerado pelo fluxo real, uma unidade Pending com data
        /// oficial. Sem arquivo de propósito: nenhum teste daqui chega a gerar PDF.
        ///
        /// Com [deliveredValidity], reproduz o cenário de renovação: a unidade gerada é entregue com aquela
        /// validade e uma pendente NOVA nasce em seguida — nessa ordem porque NewDocumentUnit reaproveita a
        /// pendente existente em vez de criar outra, e o documento acabaria com uma unidade só.
        /// </summary>
        private async Task<ScheduleSeed> SeedSignableDocumentAsync(CancellationToken ct, DateOnly? deliveredValidity = null)
        {
            var context = GetContext();

            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var documentGroup = await context.InsertDocumentGroup(company.Id, ct);

            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Signable", "Description Signable", company.Id,
                (double?)null, null,
                templateFileInfo: null,
                acceptsSignature: true, placeSignatures: [], documentGroupId: documentGroup.Id);
            await context.DocumentTemplates.AddAsync(template, ct);
            await context.SaveChangesAsync(ct);

            var employee = await context.InsertEmployeeActive(company.Id, role.Id, ct);

            var requireDocuments = RequireDocuments.Create(Guid.NewGuid(), company.Id, [role.Id],
                AssociationType.Role, "Signable Doc", "Signable Description", [], [template.Id]);
            await context.RequireDocuments.AddAsync(requireDocuments, ct);
            await context.SaveChangesAsync(ct);

            using (var scope = _factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                await service.GenerateDocumentUnitsForRequireDocument(requireDocuments.Id, company.Id, ct);
            }

            var documentId = await FindDocumentIdAsync(employee.Id, template.Id, ct);
            var generatedUnitId = Assert.Single((await GetDocumentAsync(documentId, ct)).DocumentsUnits).Id;

            var pendingUnitId = generatedUnitId;

            await WithDocumentAsync(documentId, ct, doc =>
            {
                if (deliveredValidity is null)
                {
                    doc.UpdateDocumentUnitDetails(generatedUnitId, Today, TimeSpan.Zero, "");
                    return;
                }

                doc.UpdateDocumentUnitDetails(generatedUnitId, Today, deliveredValidity, "");
                doc.InsertUnitWithoutRequireValidation(generatedUnitId, "file", "pdf");

                pendingUnitId = doc.NewDocumentUnit(Guid.NewGuid()).Id;
                doc.UpdateDocumentUnitDetails(pendingUnitId, Today, TimeSpan.Zero, "");
            });

            return new ScheduleSeed(company.Id, employee.Id, documentId, pendingUnitId);
        }

        private async Task<Guid> FindDocumentIdAsync(Guid employeeId, Guid templateId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.AsNoTracking()
                .FirstAsync(x => x.EmployeeId == employeeId && x.DocumentTemplateId == templateId, ct);
            return document.Id;
        }

        private Task DeliverUnitAsync(ScheduleSeed seed, CancellationToken ct)
            => WithDocumentAsync(seed.DocumentId, ct,
                doc => doc.InsertUnitWithoutRequireValidation(seed.DocumentUnitId, "file", "pdf"));

        private async Task WithDocumentAsync(Guid documentId, CancellationToken ct, Action<Document> mutate)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            mutate(document);
            await context.SaveChangesAsync(ct);
        }

        private async Task<DocumentUnit> GetUnitAsync(ScheduleSeed seed, CancellationToken ct)
        {
            var document = await GetDocumentAsync(seed.DocumentId, ct);
            return document.DocumentsUnits.First(x => x.Id == seed.DocumentUnitId);
        }

        private async Task<HttpResponseMessage> ScheduleAsync(ScheduleSeed seed, DateOnly sendOn, DateOnly dateLimitToSign)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);

            var model = new ScheduleDocumentToSignModel(seed.DocumentUnitId, seed.DocumentId, seed.EmployeeId,
                sendOn, dateLimitToSign, 0);

            return await client.PostAsJsonAsync($"/api/v1/{seed.CompanyId}/document/schedule/send2sign", model);
        }

        private async Task<HttpResponseMessage> CancelAsync(ScheduleSeed seed)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);

            var model = new CancelScheduledDocumentToSignModel(seed.DocumentUnitId, seed.DocumentId, seed.EmployeeId);

            return await client.PostAsJsonAsync($"/api/v1/{seed.CompanyId}/document/schedule/send2sign/cancel", model);
        }

        private async Task<Application.Queries.Document.DocumentDtos.DocumentDto> GetDocumentDtoAsync(
            ScheduleSeed seed, int? statusId = null)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);

            var url = $"/api/v1/{seed.CompanyId}/document/{seed.EmployeeId}/{seed.DocumentId}";
            if (statusId.HasValue)
                url += $"?statusId={statusId}";

            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");

            return await response.Content.ReadFromJsonAsync<Application.Queries.Document.DocumentDtos.DocumentDto>()
                ?? throw new ArgumentNullException(nameof(response));
        }

        private async Task FireScheduledSendAsync(ScheduleSeed seed, DateOnly expectedSendOn, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISignDocumentService>();
            await service.SendScheduledDocumentToSign(seed.DocumentUnitId, seed.DocumentId, seed.CompanyId, expectedSendOn, ct);
        }
    }
}
