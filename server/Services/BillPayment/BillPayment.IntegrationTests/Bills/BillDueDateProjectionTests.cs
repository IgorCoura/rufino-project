namespace BillPayment.IntegrationTests.Bills;

using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Outbox;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Teste de regressão do bug de 2026-08-27: boletos só-Pix listavam sem vencimento, sem valor
/// consolidado e sem beneficiário, mesmo com o decode oficial devolvendo os três — a projeção
/// da lista só lia a linha digitável.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillDueDateProjectionTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset ConsultedAt = new(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PixDueDate = new(2026, 6, 25);

    private const string BeneficiaryCnpj = "11222333000181";
    private const string BeneficiaryName = "PADARIA SAO JOSE LTDA";

    private const string StaticPix =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia52040000530398654071500.005802BR5912SABESP TESTE6009SAO PAULO62120508TXID000163046665";

    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;
    private readonly HttpClient _client;

    public BillDueDateProjectionTests(IntegrationTestWebAppFactory factory) : base(factory)
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

    // Boleto só-Pix verificado aparece na LISTA com o vencimento, o valor e o beneficiário que
    // o decode devolveu — e a coluna materializada due_date fica gravada no banco.
    [Fact]
    public async Task GetList_OnAPixOnlyValidatedBill_ShouldProjectDueDateAmountAndBeneficiaryFromTheDecode()
    {
        _lookups.PixResult = ResolvedPix();

        var billId = await ImportPixAsync();
        await DrainOutboxAsync();

        var page = await _client.GetFromJsonAsync<BillPageContract>(
            new Uri($"/api/v1/{TenantId}/bills", UriKind.Relative));

        var item = Assert.Single(page!.Items, b => b.Id == billId);
        Assert.Equal(PixDueDate.ToDateTime(TimeOnly.MinValue), item.DueDate);
        Assert.Equal(153.20m, item.Amount);
        Assert.NotNull(item.Beneficiary);
        Assert.Equal(BeneficiaryName, item.Beneficiary!.Name);

        var persisted = await ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .SingleAsync(b => b.Id == BillId.From(billId)));
        Assert.Equal(PixDueDate, persisted.DueDate);
    }

    // Sem consulta resolvida o boleto só-Pix continua sem vencimento — ausência honesta, não
    // data inventada.
    [Fact]
    public async Task GetList_OnAPixOnlyBillWithoutLookup_ShouldKeepDueDateNull()
    {
        var billId = await ImportPixAsync();
        await DrainOutboxAsync();

        var page = await _client.GetFromJsonAsync<BillPageContract>(
            new Uri($"/api/v1/{TenantId}/bills", UriKind.Relative));

        var item = Assert.Single(page!.Items, b => b.Id == billId);
        Assert.Null(item.DueDate);
    }

    private static PixLookupResult ResolvedPix()
        => PixLookupResult.Resolved(
            PixLookupSnapshot.Create(
                LookupParty.From(BeneficiaryName, null, BeneficiaryCnpj),
                ConsultedAt,
                isDynamic: true,
                totalAmount: new Money(153.20m, Currency.BRL),
                dueDate: PixDueDate),
            ConsultedAt);

    private async Task<Guid> ImportPixAsync()
    {
        var response = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/bills/import", UriKind.Relative),
            new ImportBillRequest(
                null, StaticPix, "ManualUpload", ReceivedAt, StorageKey: "tenant/2026-06/boleto.pdf"),
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None);
        return body!.Id;
    }

    private async Task DrainOutboxAsync()
    {
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        while (await processor.ProcessPendingAsync(CancellationToken.None) > 0)
        {
        }
    }
}
