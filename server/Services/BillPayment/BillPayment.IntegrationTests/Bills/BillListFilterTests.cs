namespace BillPayment.IntegrationTests.Bills;

using System.Net.Http.Json;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;

/// <summary>
/// O filtro <c>?status=</c> da lista de boletos — a fila de aprovação da UI depende dele.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillListFilterTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = TestTenants.Primary;
    private static readonly Guid DeciderId = new("0195a1f0-0000-7000-8000-00000000000a");
    private static readonly DateTime ReceivedAt = new(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);

    // Instrumentos sintéticos com DVs corretos — instrumento real não entra no repositório.
    private const string FirstLine = "34191234546789012345767890123457314880000061507";
    private const string SecondLine = "03399876534321098765743210987657414930000140980";
    private const string ThirdLine = "826600000010224812345672890123456786901234567898";

    private const string StaticPix =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia52040000530398654071500.005802BR5912SABESP TESTE6009SAO PAULO62120508TXID000163046665";

    public BillListFilterTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Filtrar por status devolve só os boletos naquele estado; os demais ficam de fora.
    [Fact]
    public async Task GetList_WithStatusFilter_ShouldReturnOnlyMatchingBills()
    {
        await ImportAsync(FirstLine);
        await ImportAsync(SecondLine);
        var cancelledId = await ImportAsync(ThirdLine);
        await CancelAsync(cancelledId);

        var captured = await ListAsync("Captured");
        var cancelled = await ListAsync("Cancelled");

        Assert.Equal(2, captured.Items.Count);
        Assert.All(captured.Items, b => Assert.Equal("Captured", b.Status));
        var single = Assert.Single(cancelled.Items);
        Assert.Equal(cancelledId, single.Id);
    }

    // O filtro casa por nome sem diferenciar caixa — o valor vem da query string do cliente.
    [Fact]
    public async Task GetList_WithLowerCasedStatus_ShouldMatchCaseInsensitively()
    {
        var cancelledId = await ImportAsync(FirstLine);
        await CancelAsync(cancelledId);

        var page = await ListAsync("cancelled");

        var single = Assert.Single(page.Items);
        Assert.Equal(cancelledId, single.Id);
    }

    // Status desconhecido devolve a lista inteira em vez de erro: valor inválido na query
    // string é engano do cliente, não motivo para 500 nem para lista vazia enganosa.
    [Fact]
    public async Task GetList_WithUnknownStatus_ShouldReturnTheWholeList()
    {
        await ImportAsync(FirstLine);
        await ImportAsync(SecondLine);

        var page = await ListAsync("NotAStatus");

        Assert.Equal(2, page.Items.Count);
    }

    // O cursor respeita o filtro: paginar com status atravessa só os boletos daquele estado,
    // sem repetir item e sem deixar o filtrado vazar para a página seguinte.
    [Fact]
    public async Task GetList_WithStatusFilterAndCursor_ShouldWalkOnlyMatchingPages()
    {
        await ImportAsync(FirstLine);
        await ImportAsync(SecondLine);
        await ImportAsync(null, StaticPix);
        var cancelledId = await ImportAsync(ThirdLine);
        await CancelAsync(cancelledId);

        var first = await ListAsync("Captured", limit: 2);
        Assert.Equal(2, first.Items.Count);
        Assert.NotNull(first.NextCursor);

        var second = await ListAsync("Captured", limit: 2, cursor: first.NextCursor);
        var remaining = Assert.Single(second.Items);
        Assert.Null(second.NextCursor);

        var seen = first.Items.Select(b => b.Id).Append(remaining.Id).ToList();
        Assert.Equal(3, seen.Distinct().Count());
        Assert.DoesNotContain(cancelledId, seen);
    }

    private async Task<BillPageContract> ListAsync(string status, int? limit = null, string? cursor = null)
    {
        var query = $"?status={Uri.EscapeDataString(status)}";
        if (limit is not null)
            query += $"&limit={limit}";
        if (cursor is not null)
            query += $"&cursor={Uri.EscapeDataString(cursor)}";

        var page = await Client.GetFromJsonAsync<BillPageContract>(
            new Uri($"/api/v1/{TenantId}/bills{query}", UriKind.Relative));

        return page!;
    }

    private async Task<Guid> ImportAsync(string? digitableLine, string? pixPayload = null)
    {
        var response = await Client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/bills/import", UriKind.Relative),
            new ImportBillRequest(
                digitableLine, pixPayload, "ManualUpload", ReceivedAt));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        return body!.Id;
    }

    private async Task CancelAsync(Guid billId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"/api/v1/{TenantId}/bills/{billId}/cancel", UriKind.Relative))
        {
            Content = JsonContent.Create(new BillDecisionRequest("importado por engano")),
        };
        request.Headers.Add("x-user-id", DeciderId.ToString());

        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
