namespace BillPayment.IntegrationTests.Bills;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Outbox;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A janela de submissão (ADR-021) vale para a DATA escolhida, não para o controle da tela que a
/// escolheu: pedir hoje pelo seletor livre é pedir hoje, e fora do horário é recusado igual.
/// </summary>
/// <remarks>
/// <para>
/// A guarda vive em <c>Bill.Schedule</c> e os dois caminhos de escrita — <c>POST /schedule</c> e
/// <c>POST /approve</c> com data — resolvem o mesmo veredito antes de chamá-la. Estes testes
/// provam isso <strong>pela borda</strong>: até aqui a recusa só era exercitada no agregado, e
/// um handler que esquecesse de resolver o veredito passaria pela suíte inteira.
/// </para>
/// <para>
/// <strong>A janela entra por configuração, em posição conhecida em relação ao relógio.</strong>
/// Com a janela real (9h–18h) o desfecho dependeria da hora em que a suíte roda — verde de
/// madrugada e vermelho à tarde, ou o contrário.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class SameDaySchedulingWindowTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid ApproverId = new("0195a1f0-0000-7000-8000-00000000000a");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string BeneficiaryCnpj = "11222333000181";
    private const string BeneficiaryName = "PADARIA SAO JOSE LTDA";

    // O fuso da POLÍTICA, como o servidor o resolve: "hoje" é o dia em São Paulo, não em UTC.
    // Entre 21h e meia-noite os dois discordam, e um teste que mandasse a data UTC pediria
    // AMANHÃ sem saber — passando por engano, porque data futura não consulta a janela.
    private static readonly TimeZoneInfo ProviderZone = new PaymentSchedulingOptions().ResolveTimeZone();

    private readonly WebApplicationFactory<Program> _open;
    private readonly WebApplicationFactory<Program> _closed;
    private readonly HttpClient _openClient;
    private readonly HttpClient _closedClient;

    public SameDaySchedulingWindowTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _open = factory.WithFakeLookups(OpenWindow());
        _closed = factory.WithFakeLookups(ClosedWindow());
        _openClient = _open.CreateClient().Authenticated();
        _closedClient = _closed.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _open.Services.GetRequiredService<FakeLookupServices>().Reset();
        _closed.Services.GetRequiredService<FakeLookupServices>().Reset();
        _openClient.Dispose();
        _closedClient.Dispose();
        _open.Dispose();
        _closed.Dispose();
    }

    // O teste-âncora do relato: a data de hoje digitada no seletor livre é recusada fora do
    // horário de envio. O bloqueio é da DATA — a sugestão "pagar hoje" some da folha, mas contorná-la
    // escolhendo a mesma data à mão não pode comprar um pagamento que a fila não vai submeter.
    [Fact]
    public async Task Schedule_ForTodayOutsideTheWindow_ShouldBeRefusedWith_BLP_BIL40()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var response = await PostAsync(_closedClient, $"{billId}/schedule", ScheduleForToday());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.BIL40", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        // E a recusa é de verdade: nem data no boleto, nem ordem a caminho do provedor.
        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Null(bill.ScheduledFor);
        Assert.Equal(0, await CountOrdersAsync(billId));
    }

    // O outro caminho de escrita, que é o do relato: "aprovar e agendar" numa transação só, com a
    // data escolhida à mão. Sem este teste, um handler que esquecesse de resolver o veredito
    // deixaria a porta aberta justamente onde ela é mais larga.
    [Fact]
    public async Task ApproveAndSchedule_ForTodayOutsideTheWindow_ShouldBeRefusedWith_BLP_BIL40()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync(
            _closedClient,
            $"{billId}/approve",
            new ApproveBillRequest(
                Today(), "confere", AcknowledgeRisk: true, AcknowledgeImmediateExecution: true));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.BIL40", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        // A aprovação cai junto, que é o desfecho certo: foi uma coisa só que a pessoa pediu.
        var bill = await LoadAsync(billId);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Null(bill.Approval);
    }

    // A contraprova: dentro do horário, hoje é data legítima. Sem ela, um "recusa sempre" passaria
    // nos dois testes acima.
    [Fact]
    public async Task Schedule_ForTodayInsideTheWindow_ShouldBeAccepted()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var response = await PostAsync(_openClient, $"{billId}/schedule", ScheduleForToday());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bill = await LoadAsync(billId);
        Assert.Equal(Today(), bill.ScheduledFor);
    }

    // E a outra metade da regra, que a janela NÃO alcança: data de outro dia é aceita a qualquer
    // hora. É o que o ADR-021 decidiu e o que a fila passou a honrar em 2026-09-10 — o veredito é
    // sobre HOJE, e um "negado" genérico travaria o agendamento inteiro fora do horário.
    [Fact]
    public async Task Schedule_ForAFutureDateOutsideTheWindow_ShouldBeAccepted()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var response = await PostAsync(
            _closedClient,
            $"{billId}/schedule",
            new ScheduleBillRequest(Today().AddDays(2), AcknowledgeImmediateExecution: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bill = await LoadAsync(billId);
        Assert.Equal(Today().AddDays(2), bill.ScheduledFor);
    }

    // A prévia do seletor livre tem de dizer a MESMA coisa que a escrita — era aqui que a tela
    // ficava otimista: ela consultava só a resolução de data, que não conhece a hora, e pintava
    // de verde uma data que o servidor recusaria. Sugestão que a folha oferece e a escrita nega
    // é a tela levando alguém a um erro já conhecido (ADR-021).
    [Fact]
    public async Task SchedulePreview_ForTodayOutsideTheWindow_ShouldSayItIsUnavailable()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var preview = await _closedClient.GetFromJsonAsync<SchedulePreviewContract>(
            new Uri($"{Route()}/{billId}/schedule-preview?date={Today():yyyy-MM-dd}", UriKind.Relative),
            CancellationToken.None);

        Assert.False(preview!.Available);
        Assert.Equal("outside_window", preview.UnavailableReason);
    }

    // Dentro do horário ela volta a ser uma data como outra qualquer.
    [Fact]
    public async Task SchedulePreview_ForTodayInsideTheWindow_ShouldBeAvailable()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var preview = await _openClient.GetFromJsonAsync<SchedulePreviewContract>(
            new Uri($"{Route()}/{billId}/schedule-preview?date={Today():yyyy-MM-dd}", UriKind.Relative),
            CancellationToken.None);

        Assert.True(preview!.Available);
        Assert.Null(preview.UnavailableReason);
    }

    // E a data de outro dia nem passa pela pergunta — a janela é sobre HOJE.
    [Fact]
    public async Task SchedulePreview_ForAFutureDateOutsideTheWindow_ShouldBeAvailable()
    {
        var billId = await ApproveWithoutSchedulingAsync();

        var preview = await _closedClient.GetFromJsonAsync<SchedulePreviewContract>(
            new Uri(
                $"{Route()}/{billId}/schedule-preview?date={Today().AddDays(2):yyyy-MM-dd}",
                UriKind.Relative),
            CancellationToken.None);

        Assert.True(preview!.Available);
    }

    private sealed record SchedulePreviewContract(
        DateOnly RequestedDate,
        DateOnly EffectiveDate,
        bool Slid,
        bool Immediate,
        bool AfterDueDate,
        bool Available,
        string? UnavailableReason);

    private static DateTime NowLocal()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ProviderZone);

    private static DateOnly Today() => DateOnly.FromDateTime(NowLocal());

    private static ScheduleBillRequest ScheduleForToday()
        => new(Today(), AcknowledgeImmediateExecution: true);

    /// <summary>Uma janela que CONTÉM a hora corrente, seja ela qual for.</summary>
    private static (string, string) OpenWindow()
        => NowLocal().Hour >= 12 ? ("12:00", "23:59:59.999") : ("00:00", "12:00");

    /// <summary>E a metade do dia oposta, que não a contém.</summary>
    private static (string, string) ClosedWindow()
        => NowLocal().Hour >= 12 ? ("00:00", "01:00") : ("12:00", "23:00");

    private async Task<Guid> ApproveWithoutSchedulingAsync()
    {
        var billId = await ImportAndValidateAsync();

        var response = await PostAsync(
            _openClient, $"{billId}/approve", new ApproveBillRequest(null, null, AcknowledgeRisk: true));
        response.EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        return billId;
    }

    private async Task<Guid> ImportAndValidateAsync()
    {
        var lookup = ResolvedBankSlip();
        _open.Services.GetRequiredService<FakeLookupServices>().BankSlipResult = lookup;
        _closed.Services.GetRequiredService<FakeLookupServices>().BankSlipResult = lookup;

        var response = await _openClient.PostAsJsonAsync(
            new Uri($"{Route()}/import", UriKind.Relative),
            new ImportBillRequest(BankSlipLine, null, "ManualUpload", ReceivedAt),
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None);
        await DrainOutboxAsync();

        return body!.Id;
    }

    private static async Task<HttpResponseMessage> PostAsync<T>(HttpClient client, string path, T payload)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{Route()}/{path}", UriKind.Relative))
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.Add("x-user-id", ApproverId.ToString());
        request.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        return await client.SendAsync(request, CancellationToken.None);
    }

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
        using var scope = _open.Services.CreateScope();
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

    private Task<int> CountOrdersAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .CountAsync(o => o.BillId == BillId.From(billId)));
}
