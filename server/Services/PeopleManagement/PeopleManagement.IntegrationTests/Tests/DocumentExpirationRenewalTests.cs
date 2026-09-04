using System.Net;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.RenewDocumentUnit;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;

namespace PeopleManagement.IntegrationTests.Tests
{
    // Vencer e renovar são coisas separadas.
    //
    // Os jobs (WarningExpirateDocument / DepreciateExpirateDocument) só movem o status da unidade: A Vencer e
    // Vencido são avisos. Quem cria a substituta é o RH, pela ação "Renovar" — e é ela que vincula a substituta
    // à substituída e consome a cota de renovações do template.
    //
    // Vencida ≠ depreciada: enquanto o substituto não é entregue a unidade fica Expired (a exigência está
    // descoberta); ela só vira Deprecated quando a entrega chega.
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentExpirationRenewalTests(PeopleManagementWebApplicationFactory factory) : BaseIntegrationTest(factory)
    {
        private static readonly DateOnly OfficialDate = new(2024, 1, 15);

        // A validade não pode nascer no passado, então tudo que precisa ATRAVESSAR o cálculo de validade
        // (ValidityDurationFor) ancora em hoje. A data fixa acima serve só ao que não vence.
        private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

        // --- Os jobs não criam nada -------------------------------------------

        [Fact]
        public async Task DepreciateExpirate_WhenAssociated_ExpiresTheUnitWithoutCreatingAReplacement()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);

            await ExpireAsync(seed.DocumentId, seed.UnitId, seed.CompanyId, ct);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(DocumentUnitStatus.Expired, unit.Status);
            Assert.Equal(DocumentStatus.Expired, result.Status);
            Assert.Equal(0, result.RenewalCount);
        }

        [Fact]
        public async Task WarningExpirate_WhenAssociated_MarksTheUnitWithoutCreatingAReplacement()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);

            await WarnAsync(seed.DocumentId, seed.UnitId, seed.CompanyId, ct);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            var unit = Assert.Single(result.DocumentsUnits);
            Assert.Equal(DocumentUnitStatus.Warning, unit.Status);
            Assert.Equal(DocumentStatus.Warning, result.Status);
        }

        // Regressão do Include filtrado: os jobs carregavam SÓ a unidade do disparo, e o RefreshDocumentStatus
        // recalculava o status do documento inteiro a partir dela. Um job chegando sobre uma unidade já
        // depreciada — entregue e superada antes do aviso — deixava o DOCUMENTO Deprecated, e como Deprecated
        // rola para "Okay" no funcionário, um documento com pendência aberta aparecia em dia.
        [Fact]
        public async Task WarningExpirate_WhenTheUnitIsNoLongerInForce_ShouldNotRewriteTheDocumentStatus()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);
            await DeprecateUnitAndLeavePendingAsync(seed.DocumentId, seed.UnitId, ct);

            await WarnAsync(seed.DocumentId, seed.UnitId, seed.CompanyId, ct);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(DocumentStatus.RequiresDocument, result.Status);
        }

        // --- Renovação manual --------------------------------------------------

        [Fact]
        public async Task Renew_FromAnExpiredUnit_CreatesALinkedPendingAndConsumesTheQuota()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);
            await ExpireAsync(seed.DocumentId, seed.UnitId, seed.CompanyId, ct);

            var response = await RenewAsync(seed);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);
            var replacement = Assert.Single(result.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
            Assert.Equal(seed.UnitId, replacement.ReplacesDocumentUnitId);
            Assert.Equal(1, result.RenewalCount);
            // A vencida continua vencida: a exigência só volta a estar coberta quando a substituta é entregue.
            Assert.Equal(DocumentUnitStatus.Expired, result.DocumentsUnits.First(u => u.Id == seed.UnitId).Status);
            Assert.Equal(DocumentStatus.Expired, result.Status);
        }

        // O caminho bom: renovar no aviso, antes de perder a cobertura. O documento continua "A Vencer" enquanto
        // a substituta está em voo — pedir a renovação no prazo não pode deixar o documento pior do que ignorá-la.
        [Fact]
        public async Task Renew_BeforeExpiration_KeepsTheDocumentWarningUntilTheReplacementIsDelivered()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);
            await WarnAsync(seed.DocumentId, seed.UnitId, seed.CompanyId, ct);

            await RenewAsync(seed);

            var afterRenew = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(DocumentStatus.Warning, afterRenew.Status);

            var replacementId = afterRenew.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending).Id;
            await MakeUnitOkAsync(seed.DocumentId, replacementId, ct);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(DocumentUnitStatus.Deprecated, result.DocumentsUnits.First(u => u.Id == seed.UnitId).Status);
            Assert.Equal(DocumentUnitStatus.OK, result.DocumentsUnits.First(u => u.Id == replacementId).Status);
            Assert.Equal(DocumentStatus.OK, result.Status);
        }

        [Fact]
        public async Task Renew_AskedTwice_ReturnsTheSameReplacementAndConsumesTheQuotaOnce()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);

            await RenewAsync(seed);
            await RenewAsync(seed);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(2, result.DocumentsUnits.Count);
            Assert.Equal(1, result.RenewalCount);
        }

        [Fact]
        public async Task Renew_FromAPendingUnit_IsRejected()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct);
            var pendingId = await DeprecateUnitAndLeavePendingAsync(seed.DocumentId, seed.UnitId, ct);

            var response = await RenewAsync(seed with { UnitId = pendingId });

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("PMD.DOC25", await response.Content.ReadAsStringAsync());
        }

        // --- Teto de ciclos de validade ----------------------------------------

        // O teto governa VALIDADE, não permissão. Esgotados os ciclos, a unidade nova nasce sem data de validade
        // — como num template sem regra de vencimento — e o documento simplesmente para de vencer.
        //
        // Também é a regressão que motivou trocar o contador de vencimentos por contador de renovações: aqui
        // NENHUMA unidade chega a vencer, porque o RH renova no prazo. Contando vencimento, a cota nunca era
        // consumida e um template limitado a 1 ciclo venceria para sempre.
        [Fact]
        public async Task Renew_WhenAlwaysDoneBeforeExpiration_ConsumesTheCycleAndTheReplacementStopsExpiring()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct, maxRenewals: 1);

            await RenewAsync(seed);

            var afterRenew = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(1, afterRenew.RenewalCount);
            var replacementId = afterRenew.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending).Id;

            await PutUnitDateAsync(seed, replacementId, Today);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Null(result.DocumentsUnits.First(u => u.Id == replacementId).Validity);
        }

        // O contraponto: dentro do teto a substituta recebe validade normalmente e o ciclo é consumido.
        [Fact]
        public async Task Renew_WhenBelowTheCap_StillRenewsAndTheReplacementKeepsExpiring()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct, maxRenewals: 2);

            var response = await RenewAsync(seed);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var afterRenew = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(1, afterRenew.RenewalCount);
            var replacementId = afterRenew.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending).Id;

            await PutUnitDateAsync(seed, replacementId, Today);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(Today.AddDays(365), result.DocumentsUnits.First(u => u.Id == replacementId).Validity);
        }

        // O beco sem saída que o teto criava: unidade VENCIDA num documento que já gastou todos os ciclos.
        // Renovar era recusado pelo teto, e vencida também não é invalidável (é a prova do período coberto) —
        // então o documento ficava parado em Vencido sem nenhuma ação possível na tela.
        //
        // O vencimento aqui é forçado pelo job porque, com os ciclos esgotados, nada mais vence sozinho: é o
        // estado que dado legado (o contador foi backfillado de vencimentos) e edição do teto no template
        // produzem em produção.
        [Fact]
        public async Task Renew_WithTheCyclesExhaustedOverAnExpiredUnit_RenewsWithoutValidityAndRecoversTheDocument()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct, maxRenewals: 1);

            await RenewAsync(seed);
            var afterRenew = await GetDocumentAsync(seed.DocumentId, ct);
            var replacementId = afterRenew.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending).Id;
            await PutUnitDateAsync(seed, replacementId, Today);
            await DeliverUnitAsync(seed.DocumentId, replacementId, ct);
            await ExpireAsync(seed.DocumentId, replacementId, seed.CompanyId, ct);

            var response = await RenewAsync(seed with { UnitId = replacementId });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var atCap = await GetDocumentAsync(seed.DocumentId, ct);
            // Não consumiu ciclo: a substituta não vence, então não há ciclo a consumir.
            Assert.Equal(1, atCap.RenewalCount);
            var lastUnit = Assert.Single(atCap.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
            Assert.Equal(replacementId, lastUnit.ReplacesDocumentUnitId);

            await PutUnitDateAsync(seed, lastUnit.Id, Today);
            await DeliverUnitAsync(seed.DocumentId, lastUnit.Id, ct);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Null(result.DocumentsUnits.First(u => u.Id == lastUnit.Id).Validity);
            Assert.Equal(DocumentUnitStatus.Deprecated, result.DocumentsUnits.First(u => u.Id == replacementId).Status);
            Assert.Equal(DocumentStatus.OK, result.Status);
        }

        // Um engano não pode travar a renovação: a substituta descartada não conta como renovação em voo
        // (LiveReplacementFor a ignora de propósito), então pedir de novo tem que funcionar — inclusive com os
        // ciclos esgotados, que era exatamente quando o teto recusava.
        [Fact]
        public async Task Renew_WithTheCyclesExhaustedAfterTheReplacementWasDiscarded_IsStillPossible()
        {
            var ct = CancellationToken.None;
            var context = GetContext();

            var seed = await SeedDocumentWithOkUnitAsync(context, ct, maxRenewals: 1);
            await ExpireAsync(seed.DocumentId, seed.UnitId, seed.CompanyId, ct);

            await RenewAsync(seed);
            var afterRenew = await GetDocumentAsync(seed.DocumentId, ct);
            var replacementId = afterRenew.DocumentsUnits.First(u => u.Status == DocumentUnitStatus.Pending).Id;
            await InvalidateUnitAsync(seed.DocumentId, replacementId, ct);

            var response = await RenewAsync(seed);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await GetDocumentAsync(seed.DocumentId, ct);
            Assert.Equal(1, result.RenewalCount);
            Assert.Equal(DocumentUnitStatus.Invalid, result.DocumentsUnits.First(u => u.Id == replacementId).Status);
            var live = Assert.Single(result.DocumentsUnits, u => u.Status == DocumentUnitStatus.Pending);
            Assert.Equal(seed.UnitId, live.ReplacesDocumentUnitId);
        }

        private sealed record RenewalSeed(Guid CompanyId, Guid DocumentId, Guid EmployeeId, Guid UnitId);

        // Semeia empresa/cargo/template + funcionário ativo associado ao cargo, um RequireDocuments associado
        // ao cargo, e um Document com uma unidade OK (elegível a vencer e a ser renovada).
        private async Task<RenewalSeed> SeedDocumentWithOkUnitAsync(
            PeopleManagementContext context, CancellationToken ct, int? maxRenewals = null)
        {
            var company = await context.InsertCompany(ct);
            var role = await context.InsertRole(company.Id, ct);
            var template = maxRenewals is null
                ? await context.InsertDocumentTemplate(company.Id, ct)
                : await InsertLimitedTemplateAsync(context, company.Id, maxRenewals.Value, ct);
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

            return new RenewalSeed(company.Id, document.Id, employee.Id, unit.Id);
        }

        // Template com vencimento limitado (renova N vezes). Difere do Mother padrão, que deriva ExpirationPolicy
        // indefinida do escalar de 365 dias.
        private static async Task<DocumentTemplate> InsertLimitedTemplateAsync(
            PeopleManagementContext context, Guid companyId, int maxRenewals, CancellationToken ct)
        {
            var documentGroup = await context.InsertDocumentGroup(companyId, ct);
            var template = DocumentTemplate.Create(
                Guid.NewGuid(), "Limited NR01", "Description Limited NR01", companyId,
                (double?)null, null,
                TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]),
                acceptsSignature: false, placeSignatures: [], documentGroupId: documentGroup.Id,
                usePreviousPeriod: false,
                policies: [new ExpirationLimitedPolicy(TimeSpan.FromDays(365), maxRenewals)]);
            await context.DocumentTemplates.AddAsync(template, ct);
            return template;
        }

        private async Task<HttpResponseMessage> RenewAsync(RenewalSeed seed)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);
            return await client.PostAsJsonAsync($"/api/v1/{seed.CompanyId}/document/documentunit/renew",
                new RenewDocumentUnitModel(seed.UnitId, seed.DocumentId, seed.EmployeeId));
        }

        // Dá a data real à unidade pelo ENDPOINT, não pelo agregado: é o caminho que passa por
        // DocumentService.ValidityDurationFor, que é onde o teto de ciclos decide se a unidade recebe validade.
        private async Task PutUnitDateAsync(RenewalSeed seed, Guid unitId, DateOnly date)
        {
            var client = CreateClient();
            client.InputHeaders([seed.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{seed.CompanyId}/document/documentunit",
                new UpdateDocumentUnitDetailsModel(unitId, seed.DocumentId, seed.EmployeeId, date));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Expected 200, got {(int)response.StatusCode}: {body}");
        }

        // Entrega sem mexer na data — ao contrário de MakeUnitOkAsync, que a reescreve com a OfficialDate fixa e
        // apagaria a validade que o teste quer observar.
        private async Task DeliverUnitAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.Include(x => x.DocumentsUnits).FirstAsync(x => x.Id == documentId, ct);
            document.InsertUnitWithoutRequireValidation(unitId, "file", "pdf");
            await context.SaveChangesAsync(ct);
        }

        // Invalidar deixa uma pendente no lugar, como sempre — o que interessa aqui é a substituta descartada
        // deixar de valer como renovação em voo.
        private async Task InvalidateUnitAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.AsNoTracking().FirstAsync(x => x.Id == documentId, ct);

            var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
            await service.InvalidateDocumentUnit(unitId, documentId, document.EmployeeId, document.CompanyId, ct);

            await scope.ServiceProvider.GetRequiredService<IDocumentRepository>().UnitOfWork.SaveChangesAsync(ct);
        }

        private async Task ExpireAsync(Guid documentId, Guid unitId, Guid companyId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
            await service.DepreciateExpirateDocument(unitId, documentId, companyId, ct);
        }

        private async Task WarnAsync(Guid documentId, Guid unitId, Guid companyId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDocumentDepreciationService>();
            await service.WarningExpirateDocument(unitId, documentId, companyId, ct);
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

        // Depreciar tira a unidade de vigência E deixa uma pendente no lugar — é como se chega a um estado com
        // pendência aberta sem passar pela renovação. Devolve o id da pendente substituta.
        private async Task<Guid> DeprecateUnitAndLeavePendingAsync(Guid documentId, Guid unitId, CancellationToken ct)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            var document = await context.Documents.AsNoTracking().FirstAsync(x => x.Id == documentId, ct);

            var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
            var replacement = await service.DeprecateDocumentUnit(unitId, documentId, document.EmployeeId,
                document.CompanyId, ct);

            await scope.ServiceProvider.GetRequiredService<IDocumentRepository>().UnitOfWork.SaveChangesAsync(ct);
            return replacement.Id;
        }
    }
}
