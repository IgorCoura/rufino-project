namespace BillPayment.IntegrationTests.Payments;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Outbox;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A fase 3 de ponta a ponta: aprovação vira ordem pelo outbox, a fila submete com a política
/// do ADR-017, a idempotência por referência impede o pagamento duplicado, e o boleto espelha
/// cada desfecho (ADR-002).
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class PaymentOrderFlowTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid ApproverId = new("0195a1f0-0000-7000-8000-00000000000a");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string BeneficiaryCnpj = "11222333000181";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;
    private readonly FakePaymentGateways _gateways;
    private readonly HttpClient _client;

    public PaymentOrderFlowTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithPaymentChain();
        _lookups = _host.Services.GetRequiredService<FakeLookupServices>();
        _gateways = _host.Services.GetRequiredService<FakePaymentGateways>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _lookups.Reset();
        _gateways.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    // O coração da fase: aprovar cria a ordem em rascunho pelo outbox — sem chamada externa —
    // com o trilho, o valor e a data pedida herdados do boleto.
    [Fact]
    public async Task Approve_ShouldCreateADraftPaymentOrderThroughTheOutbox()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();

        var order = await LoadOrderOfAsync(billId);

        Assert.Equal(PaymentOrderStatus.Draft, order.Status);
        Assert.Equal(PaymentOrderHold.None, order.Hold);
        Assert.Equal(PaymentRail.Boleto, order.Rail);
        Assert.Equal(ScheduleDate(), order.RequestedScheduleDate);
        Assert.Equal(615.07m, order.Amount!.Amount);
        Assert.Equal(0, _gateways.SubmissionCalls);
    }

    // Regressão do ADR-017 pela borda: boleto vencido sem o aceite de execução imediata é
    // recusado com BLP.BIL35 — e com o aceite, aprova.
    [Fact]
    public async Task Approve_AnOverdueBill_ShouldRequireTheImmediateExecutionAcknowledgment()
    {
        await LinkPaymentAccountAsync();
        var billId = await ImportAndValidateAsync(OverdueSnapshot());

        var refused = await PostBillAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("BLP.BIL35", await refused.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);

        var accepted = await PostBillAsync(
            $"{billId}/approve",
            new ApproveBillRequest(ScheduleDate(), null, AcknowledgeImmediateExecution: true));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        // O consentimento dado na aprovação viaja até a ordem — a fila não pergunta de novo.
        await DrainOutboxAsync();
        var order = await LoadOrderOfAsync(billId);
        Assert.Equal(ApproverId, order.ConfirmedBy!.Value.Value);
    }

    // A fila submete: reivindicação atômica, gateway recebe a NOSSA referência, e o boleto
    // espelha o agendamento com a data efetiva (Approved → Scheduled).
    [Fact]
    public async Task SubmitQueue_ShouldScheduleTheOrderAndMirrorTheBill()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        var claimed = await ClaimAsync();
        Assert.Equal(orderId, Assert.Single(claimed).PaymentOrderId);

        var outcome = await SubmitAsync(orderId);
        Assert.Equal("Submitted", outcome);
        await DrainOutboxAsync();

        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Pending, order.Status);
        Assert.Equal("pay_fake_1", order.ProviderOrderId);
        Assert.Equal(order.Id.Value.ToString(), _gateways.LastExternalReference);
        Assert.NotNull(_gateways.LastScheduleDate);

        var bill = await LoadBillAsync(billId);
        Assert.Equal(BillStatus.Scheduled, bill.Status);
        Assert.Equal(orderId, bill.PaymentOrderId!.Value.Value);
        Assert.Equal(order.EffectiveScheduleDate, bill.ScheduledFor);
    }

    // Tenant sem conta de pagamento: a ordem nasce RETIDA e fora da fila — estado visível, não
    // erro — e vincular a chave a devolve pela própria varredura (ADR-016).
    [Fact]
    public async Task Approve_WithoutAPaymentAccount_ShouldHoldTheOrderUntilTheKeyIsLinked()
    {
        var billId = await ApproveFutureDueBillAsync();

        var order = await LoadOrderOfAsync(billId);
        Assert.Equal(PaymentOrderHold.AwaitingAccount, order.Hold);
        Assert.Empty(await ClaimAsync());

        await LinkPaymentAccountAsync();
        await ReleaseAccountHoldAsync(order.Id.Value);

        Assert.Equal(PaymentOrderHold.None, (await LoadOrderAsync(order.Id.Value)).Hold);
        Assert.Equal(order.Id.Value, Assert.Single(await ClaimAsync()).PaymentOrderId);
    }

    // A idempotência que impede o pagamento duplicado: a retentativa COMEÇA pela consulta por
    // externalReference, e achando a ordem no provedor a adota SEM reenviar — provado por
    // contador, o reenvio nunca acontece.
    [Fact]
    public async Task SubmitRetry_ShouldAdoptTheExistingProviderOrderInsteadOfResending()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        _gateways.ScriptedSubmission = PaymentSubmissionResult.Unavailable("timeout", null);
        await ClaimAsync();

        var unavailable = await Assert.ThrowsAsync<DomainException>(() => SubmitRawAsync(orderId));
        Assert.Equal("BLP.PMO18", unavailable.Id);
        await RecordTransientFailureAsync(orderId);

        await ClaimAsync();
        _gateways.ScriptedFind = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_adopted", PaymentOrderStatus.Pending, "PENDING", ScheduleDate(), null, null, [], null));
        _gateways.ScriptedSubmission = PaymentSubmissionResult.Refused("must_not_resend", null);

        var outcome = await SubmitAsync(orderId);

        Assert.Equal("Submitted", outcome);
        Assert.Equal(1, _gateways.SubmissionCalls);
        Assert.Equal(1, _gateways.FindCalls);
        Assert.Equal("pay_adopted", (await LoadOrderAsync(orderId)).ProviderOrderId);
    }

    // O provedor recusou: a ordem desiste com o motivo visível, e o boleto espelha Failed.
    [Fact]
    public async Task Submit_WhenTheProviderRefuses_ShouldFailTheOrderAndMirrorTheBill()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        await ClaimAsync();
        _gateways.ScriptedSubmission = PaymentSubmissionResult.Refused("invalid_bank_slip", "boleto invalido");

        var outcome = await SubmitAsync(orderId);
        Assert.Equal("Refused", outcome);
        await DrainOutboxAsync();

        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Failed, order.Status);
        Assert.Contains(order.FailReasons, r => r.Contains("invalid_bank_slip", StringComparison.Ordinal));
        Assert.Equal(BillStatus.Failed, (await LoadBillAsync(billId)).Status);
    }

    // O espelho do desfecho final: a conciliação aplica Paid na ordem e o boleto vira Paid.
    [Fact]
    public async Task ApplyPaid_ShouldMirrorThePaymentOnTheBill()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        await ClaimAsync();
        await SubmitAsync(orderId);
        await DrainOutboxAsync();

        var paidAt = DateOnly.FromDateTime(DateTime.UtcNow);
        await MutateOrderAsync(orderId, order => order.ApplyProviderStatus(
            PaymentOrderStatus.Paid, paidAt, fee: null, null, DateTimeOffset.UtcNow, DateTime.UtcNow));
        await DrainOutboxAsync();

        var bill = await LoadBillAsync(billId);
        Assert.Equal(BillStatus.Paid, bill.Status);
        Assert.Equal(paidAt, (await LoadOrderAsync(orderId)).PaidAt);
    }

    // A retenção "aguardando confirmação" destrava pela borda, com o autor do token na trilha.
    [Fact]
    public async Task ConfirmImmediateEndpoint_ShouldReleaseTheHoldAndRecordTheAuthor()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        await MutateOrderAsync(orderId, order => order.HoldForConfirmation(DateTime.UtcNow));

        var response = await PostPaymentAsync($"{orderId}/confirm-immediate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderHold.None, order.Hold);
        Assert.Equal(ApproverId, order.ConfirmedBy!.Value.Value);
    }

    // A janela de reação em ação: cancelar um rascunho é local — o provedor nem fica sabendo.
    [Fact]
    public async Task CancelEndpoint_OnADraftOrder_ShouldCancelLocally()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        var response = await PostPaymentAsync($"{orderId}/cancel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentOrderStatus.Cancelled, (await LoadOrderAsync(orderId)).Status);
        Assert.Equal(0, _gateways.CancelCalls);
    }

    // A reentrega at-least-once do outbox: o MESMO comando de criação entregue duas vezes não
    // cria segunda ordem — a consulta por ordem ativa (e o índice único parcial) seguram a duplicata.
    [Fact]
    public async Task CreateCommandRedelivered_ShouldNotCreateASecondOrder()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        await LoadOrderOfAsync(billId);

        var outcome = await SendAsync(new CreatePaymentOrderForBillCommand(
            TenantId, billId, ApproverId, ScheduleDate(), AcknowledgedImmediateExecution: false));

        Assert.Equal("AlreadyExists", outcome.Outcome);
        var orders = await ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .Where(o => o.BillId == BillId.From(billId))
            .CountAsync());
        Assert.Equal(1, orders);
    }

    // Cancelar DEPOIS da submissão pergunta ao provedor primeiro, e o desfecho reflete nos dois
    // agregados: a ordem vira Cancelled e o boleto sai de Scheduled pelo espelho.
    [Fact]
    public async Task CancelEndpoint_OnAPendingOrder_ShouldAskTheProviderAndMirrorTheBill()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        await ClaimAsync();
        await SubmitAsync(orderId);
        await DrainOutboxAsync();
        Assert.Equal(BillStatus.Scheduled, (await LoadBillAsync(billId)).Status);

        var response = await PostPaymentAsync($"{orderId}/cancel");
        await DrainOutboxAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _gateways.CancelCalls);
        Assert.Equal(PaymentOrderStatus.Cancelled, (await LoadOrderAsync(orderId)).Status);
        Assert.Equal(BillStatus.Cancelled, (await LoadBillAsync(billId)).Status);
    }

    // O provedor recusou o cancelamento (a ordem já anda): 409 BLP.PMO20 e NADA muda localmente —
    // fingir o cancelamento deixaria a ordem "cancelada" pagando de verdade.
    [Fact]
    public async Task CancelEndpoint_WhenTheProviderRefuses_ShouldRespond409AndKeepTheOrder()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        await ClaimAsync();
        await SubmitAsync(orderId);
        _gateways.ScriptedCancel = PaymentCancellationResult.Refused("not_cancellable");

        var response = await PostPaymentAsync($"{orderId}/cancel");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.PMO20", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
        Assert.Equal(PaymentOrderStatus.Pending, (await LoadOrderAsync(orderId)).Status);
    }

    // Provedor fora do ar no cancelamento é 409 BLP.PMO19 — retentável, distinto da recusa.
    [Fact]
    public async Task CancelEndpoint_WhenTheProviderIsUnavailable_ShouldRespond409()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        await ClaimAsync();
        await SubmitAsync(orderId);
        _gateways.ScriptedCancel = PaymentCancellationResult.Unavailable("timeout");

        var response = await PostPaymentAsync($"{orderId}/cancel");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.PMO19", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
        Assert.Equal(PaymentOrderStatus.Pending, (await LoadOrderAsync(orderId)).Status);
    }

    // Confirmar execução imediata numa ordem que não está retida é conflito (BLP.PMO06) — o
    // endpoint não é um "destravar qualquer coisa".
    [Fact]
    public async Task ConfirmImmediateEndpoint_WithoutAPendingHold_ShouldRespond409()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        var response = await PostPaymentAsync($"{orderId}/confirm-immediate");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.PMO06", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
    }

    // A leitura da fila operacional: a lista filtra por status e o detalhe sai pelo boleto.
    [Fact]
    public async Task PaymentEndpoints_ShouldListAndResolveByBill()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;

        var page = await _client.GetFromJsonAsync<PaymentOrderPageContract>(
            new Uri($"/api/v1/{TenantId}/payments?status=Draft", UriKind.Relative), CancellationToken.None);
        Assert.Contains(page!.Items, o => o.Id == orderId);

        var byBill = await _client.GetFromJsonAsync<PaymentOrderContract>(
            new Uri($"/api/v1/{TenantId}/payments/by-bill/{billId}", UriKind.Relative), CancellationToken.None);
        Assert.Equal(orderId, byBill!.Id);
        Assert.Equal("Draft", byBill.Status);
    }

    // A guarda da corrida cancelar×submeter pela borda: com a ordem reivindicada (aluguel
    // vigente) o worker pode estar falando com o provedor NESTE instante — cancelar responde
    // 409 BLP.PMO22 e nada muda; a janela fecha sozinha quando o aluguel vence.
    [Fact]
    public async Task CancelEndpoint_WhileTheOrderIsClaimedForSubmission_ShouldRespond409()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        await ClaimAsync();

        var response = await PostPaymentAsync($"{orderId}/cancel");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.PMO22", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
        Assert.Equal(PaymentOrderStatus.Draft, (await LoadOrderAsync(orderId)).Status);
    }

    // Regressão do consentimento forjado (ADR-017): aprovado ANTES de vencer (evento sem o
    // aceite) e entregue DEPOIS do vencimento, a ordem nasce SEM consentimento e a fila para em
    // AwaitingConfirmation — o handler nunca re-deriva o "vencido" pela data da entrega.
    [Fact]
    public async Task DelayedOrderCreation_ShouldNotForgeTheImmediateConsent()
    {
        await LinkPaymentAccountAsync();
        var billId = await ImportAndValidateAsync(OverdueSnapshot());
        var approve = await PostBillAsync(
            $"{billId}/approve",
            new ApproveBillRequest(ScheduleDate(), null, AcknowledgeImmediateExecution: true));
        approve.EnsureSuccessStatusCode();

        // Simula a entrega atrasada do outbox: o flag que viaja é o que valia na APROVAÇÃO —
        // aqui, um boleto que ainda não estava vencido e cuja caixa de aceite nem apareceu.
        var outcome = await SendAsync(new CreatePaymentOrderForBillCommand(
            TenantId, billId, ApproverId, ScheduleDate(), AcknowledgedImmediateExecution: false));
        Assert.Equal("Created", outcome.Outcome);

        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        Assert.Null((await LoadOrderAsync(orderId)).ConfirmedBy);

        await ClaimAsync();
        var submit = await SubmitAsync(orderId);

        Assert.Equal("Held", submit);
        Assert.Equal(PaymentOrderHold.AwaitingConfirmation, (await LoadOrderAsync(orderId)).Hold);
        Assert.Equal(0, _gateways.SubmissionCalls);
    }

    // O rascunho cancelado reflete no boleto: sem ordem para executar a aprovação, ele volta a
    // AwaitingApproval (nunca fica Approved para sempre) e a nova decisão cria ordem NOVA.
    [Fact]
    public async Task CancelledDraft_ShouldReturnTheBillToApprovalAndAllowANewOrder()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var firstOrderId = (await LoadOrderOfAsync(billId)).Id.Value;

        (await PostPaymentAsync($"{firstOrderId}/cancel")).EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        var bill = await LoadBillAsync(billId);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Null(bill.PaymentOrderId);

        var response = await PostBillAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null, AcknowledgeRisk: true));
        response.EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        var orders = await ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .Where(o => o.BillId == BillId.From(billId))
            .CountAsync());
        Assert.Equal(2, orders);
    }

    // A compensação da corrida perdida: a ordem local morreu Cancelled mas o provedor ACEITOU a
    // submissão — o comando consulta por externalReference e cancela LÁ, nunca em silêncio.
    [Fact]
    public async Task CompensateRace_WhenThePaymentIsLiveAtTheProvider_ShouldCancelItThere()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        (await PostPaymentAsync($"{orderId}/cancel")).EnsureSuccessStatusCode();

        _gateways.ScriptedFind = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_live_1", PaymentOrderStatus.Pending, "PENDING", ScheduleDate(), null, null, [], null));

        var outcome = await CompensateAsync(orderId);

        Assert.Equal("CancelledAtProvider", outcome);
        Assert.Equal(1, _gateways.CancelCalls);
    }

    // Contraprova da compensação: o provedor não conhece a referência — nada chegou lá, nada a
    // cancelar, nenhum pedido de cancelamento é feito.
    [Fact]
    public async Task CompensateRace_WhenNothingReachedTheProvider_ShouldDoNothing()
    {
        await LinkPaymentAccountAsync();
        var billId = await ApproveFutureDueBillAsync();
        var orderId = (await LoadOrderOfAsync(billId)).Id.Value;
        (await PostPaymentAsync($"{orderId}/cancel")).EnsureSuccessStatusCode();

        var outcome = await CompensateAsync(orderId);

        Assert.Equal("NothingAtProvider", outcome);
        Assert.Equal(0, _gateways.CancelCalls);
    }

    // A prévia do sheet de aprovar: pedir um sábado devolve a data efetiva deslizada para o
    // dia útil seguinte, com o deslize explícito ANTES de o aprovador autorizar.
    [Fact]
    public async Task SchedulePreview_OnANonWorkingDay_ShouldExposeTheSlide()
    {
        var billId = await ImportAndValidateAsync(FutureDueSnapshot());
        var saturday = NextSaturdayAtLeastAWeekAhead();

        var preview = await _client.GetFromJsonAsync<SchedulePreviewContract>(
            BillsRoute($"{billId}/schedule-preview?date={saturday:yyyy-MM-dd}"), CancellationToken.None);

        Assert.NotNull(preview);
        Assert.Equal(saturday, preview!.RequestedDate);
        Assert.True(preview.EffectiveDate > saturday);
        Assert.True(preview.Slid);
        Assert.False(preview.Immediate);
    }

    // Boleto vencido: a prévia diz IMEDIATO — é a mesma resposta que a fila daria, e o que faz
    // a caixa de aceite do ADR-017 aparecer com a explicação certa.
    [Fact]
    public async Task SchedulePreview_OnAnOverdueBill_ShouldSayImmediate()
    {
        var billId = await ImportAndValidateAsync(OverdueSnapshot());

        var preview = await _client.GetFromJsonAsync<SchedulePreviewContract>(
            BillsRoute($"{billId}/schedule-preview?date={ScheduleDate():yyyy-MM-dd}"), CancellationToken.None);

        Assert.True(preview!.Immediate);
        Assert.False(preview.Slid);
    }

    // A query é obrigatória; e o boleto de um tenant não responde pela rota do outro.
    [Fact]
    public async Task SchedulePreview_WithoutADateOrThroughAnotherTenant_ShouldRefuse()
    {
        var billId = await ImportAndValidateAsync(FutureDueSnapshot());

        var missingDate = await _client.GetAsync(
            BillsRoute($"{billId}/schedule-preview"), CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, missingDate.StatusCode);

        var otherTenant = await _client.GetAsync(
            new Uri(
                $"/api/v1/{TestTenants.Secondary}/bills/{billId}/schedule-preview?date={ScheduleDate():yyyy-MM-dd}",
                UriKind.Relative),
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, otherTenant.StatusCode);
    }

    private sealed record SchedulePreviewContract(
        DateOnly RequestedDate, DateOnly EffectiveDate, bool Slid, bool Immediate);

    private static DateOnly NextSaturdayAtLeastAWeekAhead()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        while (date.DayOfWeek != DayOfWeek.Saturday)
            date = date.AddDays(1);
        return date;
    }

    private static DateOnly ScheduleDate() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);

    private static Uri BillsRoute(string path) => new($"/api/v1/{TenantId}/bills/{path}", UriKind.Relative);

    /// <summary>Retrato com vencimento FUTURO — o caminho agendável da política do ADR-017.</summary>
    private static BillLookupResult FutureDueSnapshot()
    {
        var at = DateTimeOffset.UtcNow;
        var line = DigitableLine.Parse(BankSlipLine, DateTime.UtcNow);

        return BillLookupResult.Resolved(
            LookupSnapshot.Create(
                LookupParty.From("PADARIA SAO JOSE LTDA", null, BeneficiaryCnpj),
                at,
                bankCode: line.BankCode,
                amount: line.Amount,
                originalAmount: line.Amount,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20)),
            at);
    }

    /// <summary>Retrato com o vencimento do próprio instrumento — passado em relógio real.</summary>
    private static BillLookupResult OverdueSnapshot()
    {
        var at = DateTimeOffset.UtcNow;
        var line = DigitableLine.Parse(BankSlipLine, DateTime.UtcNow);

        return BillLookupResult.Resolved(
            LookupSnapshot.Create(
                LookupParty.From("PADARIA SAO JOSE LTDA", null, BeneficiaryCnpj),
                at,
                bankCode: line.BankCode,
                amount: line.Amount,
                originalAmount: line.Amount,
                dueDate: line.DueDate is { } due ? DateOnly.FromDateTime(due) : null,
                isOverdue: true),
            at);
    }

    private async Task LinkPaymentAccountAsync()
    {
        var register = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payer-profile", UriKind.Relative),
            new RegisterPayerProfileRequest("Company", "RUFINO", "11444777000161"),
            CancellationToken.None);
        register.EnsureSuccessStatusCode();

        var link = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payer-profile/asaas-account", UriKind.Relative),
            new LinkAsaasAccountRequest("$aact_test_chave_do_tenant"),
            CancellationToken.None);
        link.EnsureSuccessStatusCode();
    }

    private async Task<Guid> ImportAndValidateAsync(BillLookupResult snapshot)
    {
        _lookups.BankSlipResult = snapshot;

        var response = await _client.PostAsJsonAsync(
            BillsRoute("import"),
            new ImportBillRequest(BankSlipLine, null, "ManualUpload", ReceivedAt),
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None);
        await DrainOutboxAsync();

        return body!.Id;
    }

    private async Task<Guid> ApproveFutureDueBillAsync()
    {
        var billId = await ImportAndValidateAsync(FutureDueSnapshot());

        // O retrato futuro contradiz o vencimento embutido na linha sintética (que é passado),
        // então o boleto sai Perigo por lookup_due_date_mismatch — o aceite de risco é o preço
        // de ter um vencimento futuro com o único instrumento fixo da suíte, e é ortogonal ao
        // que estes testes afirmam.
        var response = await PostBillAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null, AcknowledgeRisk: true));
        if (!response.IsSuccessStatusCode)
        {
            // O corpo carrega o id do erro de domínio — sem ele, um 409 aqui é indiagnosticável.
            throw new InvalidOperationException(
                $"Aprovação falhou com {(int)response.StatusCode}: "
                + await response.Content.ReadAsStringAsync(CancellationToken.None));
        }

        await DrainOutboxAsync();

        return billId;
    }

    private async Task<HttpResponseMessage> PostBillAsync<T>(string path, T payload) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BillsRoute(path))
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.Add("x-user-id", ApproverId.ToString());
        request.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private async Task<HttpResponseMessage> PostPaymentAsync(string path)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri($"/api/v1/{TenantId}/payments/{path}", UriKind.Relative));

        request.Headers.Add("x-user-id", ApproverId.ToString());
        request.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimAsync()
    {
        using var scope = _host.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();

        return await queries.ClaimPendingSubmissionsAsync(
            10, DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);
    }

    private async Task<string> SubmitAsync(Guid orderId)
        => (await SubmitRawAsync(orderId)).Outcome;

    private async Task<SubmitPaymentOrderResponse> SubmitRawAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(new SubmitPaymentOrderCommand(TenantId, orderId), CancellationToken.None);
    }

    private async Task<CreatePaymentOrderForBillResponse> SendAsync(CreatePaymentOrderForBillCommand command)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(command, CancellationToken.None);
    }

    private async Task<string> CompensateAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CompensatePaymentSubmissionRaceCommand(TenantId, orderId), CancellationToken.None);

        return result.Outcome;
    }

    private async Task ReleaseAccountHoldAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(
            new ReleasePaymentOrderAccountHoldCommand(TenantId, orderId), CancellationToken.None);
    }

    private async Task RecordTransientFailureAsync(Guid orderId)
        => await MutateOrderAsync(orderId, order => order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 5, TimeSpan.Zero, DateTime.UtcNow));

    private async Task MutateOrderAsync(Guid orderId, Action<PaymentOrder> mutate)
    {
        using var scope = _host.Services.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IPaymentOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var order = await orders.GetAsync(
            Domain.SharedKernel.TenantId.From(TenantId),
            PaymentOrderId.From(orderId),
            CancellationToken.None);

        mutate(order!);
        await unitOfWork.SaveEntitiesAsync(CancellationToken.None);
    }

    private async Task DrainOutboxAsync()
    {
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        while (await processor.ProcessPendingAsync(CancellationToken.None) > 0)
        {
        }
    }

    private Task<PaymentOrder> LoadOrderAsync(Guid orderId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .SingleAsync(o => o.Id == PaymentOrderId.From(orderId)));

    private Task<PaymentOrder> LoadOrderOfAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .SingleAsync(o => o.BillId == BillId.From(billId)));

    private Task<Bill> LoadBillAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .Include(b => b.Checks)
            .SingleAsync(b => b.Id == BillId.From(billId)));
}
