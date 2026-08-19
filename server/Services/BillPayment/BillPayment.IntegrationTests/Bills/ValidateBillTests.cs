namespace BillPayment.IntegrationTests.Bills;

using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Outbox;
using BillPayment.Infra.Persistence;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O fluxo inteiro: importar → outbox → consultar → verificar → persistir. Sem atalhos — o
/// evento de captura é despachado pelo processador de outbox de verdade.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ValidateBillTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = new("0195a1f0-0000-7000-8000-000000000002");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset ConsultedAt = new(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string BeneficiaryCnpj = "11222333000181";
    private const string OtherCnpj = "11444777000161";
    private const string BeneficiaryName = "PADARIA SAO JOSE LTDA";

    private const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;
    private readonly HttpClient _client;

    public ValidateBillTests(IntegrationTestWebAppFactory factory) : base(factory)
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

    // Caminho completo: o boleto entra, o outbox dispara a verificação, as doze checagens são
    // gravadas e o boleto fica aguardando aprovação.
    [Fact]
    public async Task ImportThenDrainOutbox_WithAConsistentBill_ShouldPersistTwelveChecksAndAwaitApproval()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();

        var billId = await ImportAsync(BankSlipLine);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Equal(12, bill.Checks.Count);
        Assert.DoesNotContain(bill.Checks, c => c.IsBlockingFailure);
        Assert.NotNull(bill.Lookup);
        Assert.Equal(BeneficiaryCnpj, bill.Lookup!.Beneficiary.TaxId!.Value);
    }

    // A consulta indisponível reprova o boleto com motivo — e nunca cai para "aprova sem consulta".
    [Fact]
    public async Task ImportThenDrainOutbox_WhenTheLookupIsUnavailable_ShouldRejectWithTheReason()
    {
        _lookups.BankSlipResult = BillLookupResult.Unavailable("timeout", null, ConsultedAt);

        var billId = await ImportAsync(BankSlipLine);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);

        Assert.Equal(BillStatus.Rejected, bill.Status);
        Assert.Contains(
            bill.Checks,
            c => c.Type == CheckType.LookupAvailability && c.ReasonCode == CheckReasons.LOOKUP_UNAVAILABLE);
    }

    // ANTIFRAUDE DE TRILHO (obrigatório, doc 03): documento híbrido cujo QR Pix aponta para um
    // CNPJ diferente do código de barras é bloqueado antes de chegar à aprovação. É o teste que
    // prova a defesa contra QR adulterado colado sobre boleto verdadeiro.
    [Fact]
    public async Task ImportThenDrainOutbox_WhenThePixQrPointsToAnotherPayee_ShouldRejectByPixBarcodeConsistency()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();
        _lookups.PixResult = ResolvedPix(OtherCnpj);

        var billId = await ImportAsync(BankSlipLine, DynamicPix);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);

        Assert.Equal(BillStatus.Rejected, bill.Status);

        var check = bill.Checks.Single(c => c.Type == CheckType.PixBarcodeConsistency);
        Assert.Equal(CheckOutcome.Failed, check.Outcome);
        Assert.Equal(CheckReasons.PIX_BARCODE_PAYEE_MISMATCH, check.ReasonCode);
        Assert.True(check.IsBlockingFailure);
    }

    // Os dois trilhos contando a mesma história não bloqueiam nada.
    [Fact]
    public async Task ImportThenDrainOutbox_WhenBothRailsAgree_ShouldNotBlock()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();
        _lookups.PixResult = ResolvedPix(BeneficiaryCnpj);

        var billId = await ImportAsync(BankSlipLine, DynamicPix);
        await DrainOutboxAsync();

        var bill = await LoadAsync(billId);

        Assert.Equal(CheckOutcome.Passed, bill.Checks.Single(c => c.Type == CheckType.PixBarcodeConsistency).Outcome);
    }

    // Revalidar substitui o retrato corrente e PRESERVA o anterior no histórico — meses depois
    // é preciso responder com que informação a aprovação foi dada.
    [Fact]
    public async Task Revalidate_ShouldReplaceTheSnapshotAndKeepThePreviousOneInHistory()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();

        var billId = await ImportAsync(BankSlipLine);
        await DrainOutboxAsync();

        _lookups.BankSlipResult = ResolvedBankSlip(amount: 700.00m, consultedAt: ConsultedAt.AddHours(6));
        await RevalidateAsync(billId);

        var bill = await LoadAsync(billId);

        Assert.Equal(700.00m, bill.Lookup!.Amount!.Amount);
        Assert.Equal(2, bill.LookupHistory.Count);
        Assert.Equal(615.07m, bill.LookupHistory[0].BankSlipSnapshot!.Amount!.Amount);
        Assert.Equal(2, _lookups.BankSlipCallCount);
    }

    // Duplicata entre tenants reprova o segundo boleto com aviso GENÉRICO — a evidência não
    // pode revelar de quem é o boleto original (ADR-008).
    [Fact]
    public async Task ImportThenDrainOutbox_WhenAnotherTenantAlreadyHasTheBill_ShouldRejectWithoutRevealingIt()
    {
        _lookups.BankSlipResult = ResolvedBankSlip();

        var firstId = await ImportAsync(BankSlipLine, tenantId: OtherTenantId);
        await DrainOutboxAsync();

        // O índice único global barra a segunda importação da mesma chave — que é a defesa
        // mais forte. O check existe para o caso em que a chave foi liberada e reimportada.
        var response = await _client.PostAsJsonAsync(ImportRoute(TenantId), Request(BankSlipLine), CancellationToken.None);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        Assert.DoesNotContain(OtherTenantId.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(firstId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    private static BillLookupResult ResolvedBankSlip(decimal amount = 615.07m, DateTimeOffset? consultedAt = null)
        => BillLookupResult.Resolved(
            LookupSnapshot.Create(
                LookupParty.From(BeneficiaryName, null, BeneficiaryCnpj),
                consultedAt ?? ConsultedAt,
                bankCode: new BankCode("341"),
                amount: new Money(amount, Currency.BRL),
                originalAmount: new Money(615.07m, Currency.BRL),
                dueDate: new DateOnly(2026, 6, 25),
                fee: new Money(1.99m, Currency.BRL)),
            consultedAt ?? ConsultedAt);

    private static PixLookupResult ResolvedPix(string receiverTaxId)
        => PixLookupResult.Resolved(
            PixLookupSnapshot.Create(
                LookupParty.From(BeneficiaryName, null, receiverTaxId),
                ConsultedAt,
                isDynamic: true,
                totalAmount: new Money(615.07m, Currency.BRL),
                dueDate: new DateOnly(2026, 6, 25)),
            ConsultedAt);

    private static Uri ImportRoute(Guid tenantId) => new($"/api/v1/{tenantId}/bills/import", UriKind.Relative);

    private static ImportBillRequest Request(string? line, string? pix = null)
        => new(line, pix, "ManualUpload", ReceivedAt, StorageKey: "tenant/2026-06/boleto.pdf");

    private async Task<Guid> ImportAsync(string? line, string? pix = null, Guid? tenantId = null)
    {
        var response = await _client.PostAsJsonAsync(
            ImportRoute(tenantId ?? TenantId), Request(line, pix), CancellationToken.None);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None);
        return body!.Id;
    }

    private async Task DrainOutboxAsync()
    {
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        // Uma mensagem por chamada, por desenho do claim — drena até esvaziar.
        while (await processor.ProcessPendingAsync(CancellationToken.None) > 0)
        {
        }
    }

    private async Task RevalidateAsync(Guid billId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Application.Mediator.IMediator>();

        await mediator.Send(
            new Application.Bills.Commands.ValidateBillCommand(TenantId, billId), CancellationToken.None);
    }

    private Task<Bill> LoadAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .Include(b => b.Checks)
            .SingleAsync(b => b.Id == BillId.From(billId)));
}
