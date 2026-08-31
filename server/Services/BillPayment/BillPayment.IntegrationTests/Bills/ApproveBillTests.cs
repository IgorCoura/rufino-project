namespace BillPayment.IntegrationTests.Bills;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Outbox;
using BillPayment.Infra.Persistence;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A decisão humana pela porta da frente: aprovar, recusar, cancelar e ler o detalhe.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ApproveBillTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid ApproverId = new("0195a1f0-0000-7000-8000-00000000000a");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string BeneficiaryCnpj = "11222333000181";
    private const string BeneficiaryName = "PADARIA SAO JOSE LTDA";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;
    private readonly HttpClient _client;

    public ApproveBillTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithFakeLookups();
        _lookups = _host.Services.GetRequiredService<FakeLookupServices>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _lookups.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    // Fluxo completo pela API: importa, verifica, aprova. A decisão fica gravada com quem
    // decidiu e com a data escolhida.
    [Fact]
    public async Task Approve_OnAValidatedBill_ShouldPersistTheDecisionAndTheScheduleDate()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), "ok"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApproveBillResponseContract>(CancellationToken.None);
        Assert.Equal("Approved", body!.Status);

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Equal(ApproverId, bill.Approval!.DecidedBy.Value);
        Assert.Equal(ScheduleDate(), bill.ScheduledFor);
    }

    // Regressão (auditoria 2026-08-28): a aprovação dispara o aprendizado de expectativa pelo
    // outbox, e o repositório fazia Include sobre a coluna jsonb dos instrumentos — estourava em
    // toda execução, a mensagem ia para dead-letter e o aprendizado nunca rodou em produção. Com
    // beneficiário resolvido, o evento de aprovação tem que ser processado sem erro nenhum.
    [Fact]
    public async Task Approve_WithAResolvedPayee_ShouldLetTheExpectationLearningRunWithoutFailing()
    {
        await RegisterBeneficiaryAsPayeeAsync();
        var billId = await ImportAndValidateAsync();
        Assert.NotNull((await LoadAsync(billId)).PayeeId);

        var response = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null));
        response.EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        var deadLetters = await ExecuteDbContextAsync(db => db.OutboxDeadLetters.AsNoTracking().CountAsync());
        var approvedEvents = await ExecuteDbContextAsync(db => db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.EventType.EndsWith(nameof(BillApprovedDomainEvent)))
            .ToListAsync());

        Assert.Equal(0, deadLetters);
        var approved = Assert.Single(approvedEvents);
        Assert.True(approved.Processed);
        Assert.Null(approved.Error);
    }

    // Beneficiário na blacklist de ponta a ponta: o boleto verifica como Perigo pelo check de
    // beneficiário, aprovar sem assumir o risco é 409 BLP.BIL27, e com o aceite explícito a
    // aprovação passa (ADR-015 — a marca sinaliza, quem decide é o humano).
    [Fact]
    public async Task Approve_WhenThePayeeIsBlacklisted_ShouldTurnDangerAndRequireTheAcknowledgment()
    {
        var payeeId = await RegisterBeneficiaryAsPayeeAsync();
        var mark = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payees/{payeeId}/standing", UriKind.Relative),
            new AlterPayeeStandingRequest("Blacklisted"),
            CancellationToken.None);
        mark.EnsureSuccessStatusCode();

        var billId = await ImportAndValidateAsync();

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Same(RiskLevel.Danger, bill.Risk);
        Assert.Contains(bill.Checks, c => c.ReasonCode == CheckReasons.PAYEE_BLACKLISTED && c.IsBlockingFailure);

        var refused = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var approved = await PostAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), "risco assumido", AcknowledgeRisk: true));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
    }

    // Regressão (auditoria 2026-08-28): sem token de concorrência, aprovadores simultâneos liam
    // AwaitingApproval, todos gravavam Approved e VÁRIOS eventos de aprovação entravam no outbox
    // — na fase de pagamento, vários pagamentos. Agora exatamente um vence; os outros recebem
    // 409 (pelo xmin, ou pela guarda de situação se leram depois do commit), e o outbox tem UM
    // evento de aprovação.
    [Fact]
    public async Task Approve_Concurrently_ShouldLetExactlyOneWinAndEmitASingleApprovedEvent()
    {
        var billId = await ImportAndValidateAsync();
        var payload = new ApproveBillRequest(ScheduleDate(), null);

        var attempts = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => PostAsync($"{billId}/approve", payload)));

        var statuses = attempts.Select(r => r.StatusCode).ToList();
        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.OK));
        Assert.All(statuses.Where(s => s != HttpStatusCode.OK), s => Assert.Equal(HttpStatusCode.Conflict, s));

        var approvedEvents = await ExecuteDbContextAsync(db => db.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.EventType.EndsWith(nameof(BillApprovedDomainEvent))));

        Assert.Equal(1, approvedEvents);
        Assert.Same(BillStatus.Approved, (await LoadAsync(billId)).Status);
    }

    // ADR-007: sem identificar quem autoriza, não há aprovação. O domínio recusa (BLP.BIL22) e
    // o filtro traduz para 400.
    [Fact]
    public async Task Approve_WithoutIdentifyingTheApprover_ShouldReturnBadRequest()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null), userId: Guid.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ADR-015: boleto em Perigo sem o aceite explícito devolve 409, e nada é gravado.
    [Fact]
    public async Task Approve_OnADangerBillWithoutAcknowledgingTheRisk_ShouldReturnConflict()
    {
        _lookups.BankSlipResult = BillLookupResult.Unavailable("timeout", null, ConsultedAt());

        var billId = await ImportAsync();
        await DrainOutboxAsync();

        var response = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Null((await LoadAsync(billId)).Approval);
    }

    // ADR-015 pela porta da frente: com acknowledgeRisk o Perigo é aprovável, e a trilha grava
    // o nível de risco que o aprovador viu no instante da decisão.
    [Fact]
    public async Task Approve_OnADangerBillAcknowledgingTheRisk_ShouldApproveAndRecordIt()
    {
        _lookups.BankSlipResult = BillLookupResult.Unavailable("timeout", null, ConsultedAt());

        var billId = await ImportAsync();
        await DrainOutboxAsync();

        var response = await PostAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), "risco assumido", AcknowledgeRisk: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Same(RiskLevel.Danger, bill.Approval!.RiskAtDecision);
    }

    // Retrato velho não sustenta aprovação — e revalidar é o caminho de volta.
    [Fact]
    public async Task Approve_WithAStaleSnapshot_ShouldReturnConflictUntilRevalidated()
    {
        _lookups.BankSlipResult = ResolvedBankSlip(consultedAt: DateTimeOffset.UtcNow.AddDays(-3));

        var billId = await ImportAsync();
        await DrainOutboxAsync();

        var stale = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        _lookups.BankSlipResult = ResolvedBankSlip();
        var revalidate = await PostAsync<object>($"{billId}/revalidate", payload: null);
        Assert.Equal(HttpStatusCode.OK, revalidate.StatusCode);

        var approved = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
    }

    // Recusar é terminal e exige motivo.
    [Fact]
    public async Task Deny_ShouldMakeTheBillTerminal()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync($"{billId}/deny", new BillDecisionRequest("não reconheço este fornecedor"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.Denied, bill.Status);
        Assert.True(bill.Status.IsTerminal);
    }

    // Cancelar alcança boleto que nem chegou a ser verificado, e libera a chave natural — o
    // mesmo documento pode ser reimportado depois.
    [Fact]
    public async Task Cancel_OnAJustImportedBill_ShouldReleaseTheNaturalKeyForReimport()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();

        var billId = await ImportAsync();

        var cancelled = await PostAsync($"{billId}/cancel", new BillDecisionRequest("importado por engano"));
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);

        var reimported = await _client.PostAsJsonAsync(ImportRoute(), Request(), CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, reimported.StatusCode);
    }

    // O detalhe traz as doze verificações com evidência — e continua sem a linha digitável.
    [Fact]
    public async Task GetDetail_ShouldReturnTheTwelveChecksWithoutLeakingThePaymentInstrument()
    {
        var billId = await ImportAndValidateAsync();

        var response = await _client.GetAsync(new Uri($"{Route()}/{billId}/detail", UriKind.Relative), CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(CancellationToken.None);
        Assert.DoesNotContain(BankSlipLine, raw, StringComparison.Ordinal);

        var detail = await response.Content.ReadFromJsonAsync<BillDetailContract>(CancellationToken.None);
        Assert.Equal(Enumeration.GetAll<CheckType>().Count(), detail!.Checks.Count);
        Assert.Equal(BeneficiaryCnpj, detail.Beneficiary!.TaxId!.Replace(".", "", StringComparison.Ordinal)
            .Replace("/", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal));
        Assert.All(detail.Checks, c => Assert.False(string.IsNullOrWhiteSpace(c.Type)));
    }

    // A mesma aprovação reenviada com o mesmo x-requestid não decide duas vezes.
    [Fact]
    public async Task Approve_ResentWithTheSameRequestId_ShouldBeIdempotent()
    {
        var billId = await ImportAndValidateAsync();
        var requestId = new Guid("0195a1f0-0000-7000-8000-0000000000aa");

        var first = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null), requestId: requestId);
        var second = await PostAsync($"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null), requestId: requestId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<ApproveBillResponseContract>(CancellationToken.None);
        Assert.Equal(Guid.Empty, body!.Id);
    }

    private static DateOnly ScheduleDate() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

    private static DateTimeOffset ConsultedAt() => DateTimeOffset.UtcNow;

    private static string Route() => $"/api/v1/{TenantId}/bills";

    private static Uri ImportRoute() => new($"{Route()}/import", UriKind.Relative);

    private static ImportBillRequest Request()
        => new(BankSlipLine, null, "ManualUpload", ReceivedAt);

    /// <summary>
    /// Retrato coerente com o código de barras sintético.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Valor e vencimento saem do <strong>próprio instrumento</strong>, não de números
    /// escolhidos à mão: inventá-los faria o check de consistência reprovar o boleto e o teste
    /// de aprovação nunca chegaria a exercitar a aprovação.
    /// </para>
    /// <para>
    /// O instante da consulta acompanha o relógio real porque as guardas de validade do retrato
    /// e de data de agendamento comparam contra "hoje" — congelá-lo faria o teste apodrecer.
    /// O vencimento do instrumento é passado, então o boleto sai da verificação com
    /// <c>DueDateSanity</c> reprovado; é <em>advisory</em>, não bloqueia, e é justamente o
    /// cenário de "aprovar apesar de um ponto de atenção".
    /// </para>
    /// </remarks>
    private static BillLookupResult ResolvedBankSlip(DateTimeOffset? consultedAt = null)
    {
        var at = consultedAt ?? DateTimeOffset.UtcNow;
        var line = DigitableLine.Parse(BankSlipLine, DateTime.UtcNow);

        return BillLookupResult.Resolved(
            LookupSnapshot.Create(
                LookupParty.From(BeneficiaryName, null, BeneficiaryCnpj),
                at,
                bankCode: line.BankCode,
                amount: line.Amount,
                originalAmount: line.Amount,
                dueDate: line.DueDate is { } due ? DateOnly.FromDateTime(due) : null,
                fee: new Money(1.99m, Currency.BRL)),
            at);
    }

    // async, e não um Task devolvido direto: com `using` num método síncrono a requisição é
    // descartada antes de o envio terminar, e o TestHost falha com ObjectDisposedException ao
    // ler o corpo.
    private async Task<HttpResponseMessage> PostAsync<T>(
        string path,
        T? payload,
        Guid? userId = null,
        Guid? requestId = null)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{Route()}/{path}", UriKind.Relative))
        {
            Content = payload is null ? null : JsonContent.Create(payload),
        };

        request.Headers.Add("x-user-id", (userId ?? ApproverId).ToString());
        request.Headers.Add("x-requestid", (requestId ?? Guid.NewGuid()).ToString());

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private async Task<Guid> ImportAsync()
    {
        var response = await _client.PostAsJsonAsync(ImportRoute(), Request(), CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None);
        return body!.Id;
    }

    private async Task<Guid> RegisterBeneficiaryAsPayeeAsync()
    {
        var response = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payees", UriKind.Relative),
            new RegisterPayeeRequest(BeneficiaryName, BeneficiaryCnpj, "Unbounded", null, null, null, null),
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PayeeIdResponse>(CancellationToken.None);
        return body!.Id;
    }

    private async Task<Guid> ImportAndValidateAsync()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();

        var billId = await ImportAsync();
        await DrainOutboxAsync();

        return billId;
    }

    // A consulta oficial sai com a credencial DO TENANT (2026-08-31): com a subconta vinculada,
    // o ponteiro do cofre chega à porta de lookup; a chave em si nunca sai do cofre.
    [Fact]
    public async Task Validate_ShouldCarryTheTenantsCredentialIntoTheLookup()
    {
        var register = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payer-profile", UriKind.Relative),
            new RegisterPayerProfileRequest("Company", "RUFINO", "11444777000161"),
            CancellationToken.None);
        register.EnsureSuccessStatusCode();

        var link = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payer-profile/asaas-account", UriKind.Relative),
            new LinkAsaasAccountRequest("$aact_prod_chave_do_tenant"),
            CancellationToken.None);
        link.EnsureSuccessStatusCode();

        await ImportAndValidateAsync();

        Assert.NotNull(_lookups.LastCredential);
        Assert.True(_lookups.LastCredential!.IsLocalVault);
    }

    private async Task DrainOutboxAsync()
    {
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        while (await processor.ProcessPendingAsync(CancellationToken.None) > 0)
        {
        }
    }

    private Task<Bill> LoadAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .Include(b => b.Checks)
            .SingleAsync(b => b.Id == BillId.From(billId)));
}
