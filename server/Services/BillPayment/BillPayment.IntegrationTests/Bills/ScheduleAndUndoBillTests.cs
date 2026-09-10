namespace BillPayment.IntegrationTests.Bills;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Infra.Outbox;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A separação aprovar/agendar, o cancelamento de agendamento que devolve a Aprovados, e a
/// reversão de decisão terminal — tudo pela porta da frente (ADR-018).
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ScheduleAndUndoBillTests : BaseIntegrationTest, IDisposable
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

    public ScheduleAndUndoBillTests(IntegrationTestWebAppFactory factory) : base(factory)
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

    // Aprovar sozinho autoriza e PARA: sem data, e sem ordem de pagamento nenhuma. É o estado
    // que põe o boleto na aba de aprovados esperando alguém mandar pagar.
    [Fact]
    public async Task Approve_WithoutADate_ShouldLeaveTheBillApprovedWithNoOrder()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync($"{billId}/approve", new ApproveBillRequest(null, "confere", AcknowledgeRisk: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApproveBillResponseContract>(CancellationToken.None);
        Assert.Equal("Approved", body!.Status);
        Assert.Null(body.ScheduledFor);

        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Null(bill.ScheduledFor);

        // O que mais importa: NENHUMA ordem foi criada. Aprovar deixou de mover dinheiro.
        Assert.Equal(0, await CountOrdersAsync(billId));
    }

    // E depois o agendamento, por endpoint próprio, com alçada própria.
    [Fact]
    public async Task Schedule_OnAnApprovedBill_ShouldStampTheDateAndCreateTheOrder()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var response = await PostAsync(
            $"{billId}/schedule",
            new ScheduleBillRequest(ScheduleDate(), AcknowledgeImmediateExecution: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);
        Assert.Equal(ScheduleDate(), bill.ScheduledFor);
        Assert.Equal(1, await CountOrdersAsync(billId));
    }

    [Fact]
    public async Task Schedule_OnABillThatWasNeverApproved_ShouldBeRefusedWith_BLP_BIL36()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync(
            $"{billId}/schedule", new ScheduleBillRequest(ScheduleDate(), AcknowledgeImmediateExecution: true));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.BIL36", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // A alçada nova: quem só aprova NÃO manda pagar.
    [Fact]
    public async Task Schedule_WithoutTheSchedulingScope_ShouldBeForbidden()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var response = await PostAsync(
            $"{billId}/schedule",
            new ScheduleBillRequest(ScheduleDate(), AcknowledgeImmediateExecution: true),
            scopes: "view,approve");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // "Aprovar e agendar" é a soma de dois poderes, e a porta de entrada só confere um: sem
    // bill:schedule, mandar a data no corpo não pode virar pagamento.
    [Fact]
    public async Task Approve_WithADate_ButWithoutTheSchedulingScope_ShouldBeForbidden()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync(
            $"{billId}/approve",
            new ApproveBillRequest(ScheduleDate(), null, AcknowledgeRisk: true, AcknowledgeImmediateExecution: true),
            scopes: "view,approve");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null((await LoadAsync(billId)).Approval);
    }

    // 🐛 A REGRESSÃO QUE ORIGINOU O ADR-018: cancelar o agendamento levava o boleto a Cancelled
    // (terminal), e quem só queria trocar a data perdia o boleto e a aprovação junto.
    [Fact]
    public async Task CancellingTheOrder_ShouldReturnTheBillToApprovedAndAllowReScheduling()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        await PostAsync(
            $"{billId}/schedule", new ScheduleBillRequest(ScheduleDate(), AcknowledgeImmediateExecution: true));
        await DrainOutboxAsync();

        var order = await SingleOrderAsync(billId);
        var cancelled = await _client.PostAsync(
            new Uri($"/api/v1/{TenantId}/payments/{order.Id.Value}/cancel", UriKind.Relative),
            null,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Null(bill.ScheduledFor);
        Assert.Null(bill.PaymentOrderId);

        // A aprovação sobreviveu — é isso que dispensa uma nova.
        Assert.NotNull(bill.Approval);
        Assert.Equal(ApprovalDecision.Approved, bill.Approval!.Decision);

        // E o boleto é agendável de novo, para outra data.
        var rescheduled = await PostAsync(
            $"{billId}/schedule",
            new ScheduleBillRequest(ScheduleDate().AddDays(1), AcknowledgeImmediateExecution: true));

        Assert.Equal(HttpStatusCode.OK, rescheduled.StatusCode);
    }

    [Fact]
    public async Task UndoDecision_OnADeniedBill_ShouldReturnItToTheQueueAndRevalidate()
    {
        var billId = await ImportAndValidateAsync();

        await PostAsync($"{billId}/deny", new BillDecisionRequest("cobrança indevida"));
        Assert.Equal(BillStatus.Denied, (await LoadAsync(billId)).Status);

        var response = await PostAsync(
            $"{billId}/undo-decision", new BillDecisionRequest("recusa por engano"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);

        // A revalidação automática rodou: as verificações foram reexecutadas pelo outbox.
        Assert.NotEmpty(bill.Checks);
    }

    // Alçada própria: aprovar, negar e cancelar NÃO dão o poder de desfazer.
    [Fact]
    public async Task UndoDecision_WithoutItsOwnScope_ShouldBeForbidden()
    {
        var billId = await ImportAndValidateAsync();
        await PostAsync($"{billId}/deny", new BillDecisionRequest("motivo"));

        var response = await PostAsync(
            $"{billId}/undo-decision",
            new BillDecisionRequest("motivo"),
            scopes: "view,approve,deny,cancel");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BillStatus.Denied, (await LoadAsync(billId)).Status);
    }

    // Só recusa e cancelamento se desfazem: boleto que AINDA espera decisão não tem o que
    // reverter. A recusa tem de ser a do agregado — o BLP.BIL38 que nomeia a regra — e não a
    // pré-condição de chave reocupada, que num boleto vivo encontra ele mesmo.
    [Fact]
    public async Task UndoDecision_OnABillAwaitingApproval_ShouldBeRefusedWith_BLP_BIL38()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync(
            $"{billId}/undo-decision", new BillDecisionRequest("motivo"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.BIL38", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // A trilha chega inteira ao detalhe, e cada mudança de estado deixou a sua linha.
    [Fact]
    public async Task Detail_ShouldCarryTheWholeTrailWithActorAndTransition()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var response = await _client.GetAsync(
            new Uri($"{Route()}/{billId}/detail", UriKind.Relative), CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var detail = await response.Content.ReadFromJsonAsync<BillDetailContract>(CancellationToken.None);
        var actions = detail!.History.Select(h => h.Action).ToList();

        Assert.Equal(["Captured", "Validated", "Approved"], actions);

        var captured = detail.History[0];
        Assert.Null(captured.FromStatus);
        Assert.Null(captured.ActorUserId);
        Assert.Equal("System", captured.Origin);
        Assert.Equal("Sistema", captured.ActorName);

        var approved = detail.History[^1];
        Assert.Equal(ApproverId, approved.ActorUserId);
        Assert.Equal("User", approved.Origin);
        Assert.Equal("AwaitingApproval", approved.FromStatus);
        Assert.Equal("Approved", approved.ToStatus);
    }

    // A pergunta que originou este teste: se cancelarem no painel do provedor, a trilha diz que
    // foi lá? Antes de 2026-09-08 não dizia — gravava "Sistema", igual a tudo. Aqui o
    // cancelamento vem pela NOSSA API, então a trilha tem de nomear a pessoa; o contraponto do
    // provedor é coberto no unitário, onde dá para forçar a origem sem um painel de verdade.
    [Fact]
    public async Task CancellingThroughOurApi_ShouldNameThePersonInTheTrail()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        await PostAsync(
            $"{billId}/schedule", new ScheduleBillRequest(ScheduleDate(), AcknowledgeImmediateExecution: true));
        await DrainOutboxAsync();

        var order = await SingleOrderAsync(billId);
        // O `x-user-id` é o que o dublê de autenticação traduz em `sub`, e é do `sub` que sai o
        // autor. Sem ele o pedido ainda cancela — só que como Guid.Empty, que a trilha descarta
        // por não ser pessoa nenhuma; a asserção de autor passaria a valer vazio.
        using var cancel = new HttpRequestMessage(
            HttpMethod.Post, new Uri($"/api/v1/{TenantId}/payments/{order.Id.Value}/cancel", UriKind.Relative));
        cancel.Headers.Add("x-user-id", ApproverId.ToString());
        (await _client.SendAsync(cancel, CancellationToken.None)).EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);

        // Ordenado, e não `History[^1]`: a coleção owned NÃO garante ordem por si — é por isso que
        // o DTO do detalhe ordena explicitamente. Lendo o último item cru, este teste passava ou
        // reprovava conforme a ordem que o Postgres devolvia, que varia com o que rodou antes na
        // suíte. Intermitência observada em 2026-09-10, em rodadas com o código idêntico.
        var entry = bill.History.OrderBy(h => h.OccurredAt).Last();

        Assert.Equal(BillAction.Unscheduled, entry.Action);
        Assert.Equal(BillActionOrigin.User, entry.Origin);
        Assert.Equal(ApproverId, entry.ActorUserId!.Value.Value);
        Assert.DoesNotContain("NO PROVEDOR", entry.Note, StringComparison.Ordinal);
    }

    private async Task<Guid> ApproveWithoutSchedulingAsync()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync($"{billId}/approve", new ApproveBillRequest(null, null, AcknowledgeRisk: true));
        response.EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        return billId;
    }

    private async Task<Guid> ImportAndValidateAsync()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();

        var response = await _client.PostAsJsonAsync(
            new Uri($"{Route()}/import", UriKind.Relative),
            new ImportBillRequest(BankSlipLine, null, "ManualUpload", ReceivedAt),
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None);
        await DrainOutboxAsync();

        return body!.Id;
    }

    private async Task<HttpResponseMessage> PostAsync<T>(
        string path,
        T? payload,
        Guid? userId = null,
        string? scopes = null)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{Route()}/{path}", UriKind.Relative))
        {
            Content = payload is null ? null : JsonContent.Create(payload),
        };

        if (scopes is not null)
            request.Headers.Add(FakeAuthorizationServerClient.ScopesHeader, scopes);

        request.Headers.Add("x-user-id", (userId ?? ApproverId).ToString());
        request.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private static DateOnly ScheduleDate() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

    private static string Route() => $"/api/v1/{TenantId}/bills";

    /// <summary>
    /// Retrato coerente com o código de barras sintético — valor e vencimento saem do PRÓPRIO
    /// instrumento, senão o check de consistência reprova e o teste nunca chega à decisão.
    /// </summary>
    private static BillLookupResult ResolvedBankSlip()
    {
        var at = DateTimeOffset.UtcNow;
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
            .Include(b => b.History)
            .SingleAsync(b => b.Id == BillId.From(billId)));

    private Task<int> CountOrdersAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .CountAsync(o => o.BillId == BillId.From(billId)));

    private Task<PaymentOrder> SingleOrderAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .SingleAsync(o => o.BillId == BillId.From(billId)));
}
