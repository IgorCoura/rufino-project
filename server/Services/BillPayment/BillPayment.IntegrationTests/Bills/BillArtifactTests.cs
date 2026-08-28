namespace BillPayment.IntegrationTests.Bills;

using System.Net;
using System.Text;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O documento original que sustenta a decisão de aprovar.
/// </summary>
/// <remarks>
/// <para>
/// A tela de aprovação mostra as doze verificações; o que faltava era o papel contra o qual
/// conferi-las. <strong>Continua valendo que a linha digitável não sai por esta API</strong> —
/// o que este endpoint serve é o arquivo como ele chegou, não os dígitos.
/// </para>
/// <para>
/// Boleto importado à mão não tem arquivo, e isso é estado normal: a resposta é 404, e a tela
/// simplesmente não oferece o botão.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillArtifactTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(TestTenants.Primary);
    private static readonly TenantId TenantB = TenantId.From(TestTenants.Secondary);
    private static readonly DateTime OccurredAt = new(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("%PDF-1.4 boleto de teste");

    private readonly WebApplicationFactory<Program> _host;
    private readonly InMemoryAttachmentStorage _storage;

    public BillArtifactTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithCaptureChain();
        _storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();
    }

    // O caso principal: boleto que veio de um artefato guardado serve o documento.
    [Fact]
    public async Task GetArtifact_WhenTheBillCameFromAStoredArtifact_ShouldServeTheDocument()
    {
        var billId = await SeedBillAsync(TenantA, withArtifact: true);

        var response = await GetAsync(TestTenants.Primary, billId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Bytes, await response.Content.ReadAsByteArrayAsync());
    }

    // O documento vai inteiro, e os dígitos continuam fora: o corpo é o arquivo, não o modelo.
    [Fact]
    public async Task GetArtifact_ShouldNotLeakTheDigitableLine()
    {
        var billId = await SeedBillAsync(TenantA, withArtifact: true);

        var response = await GetAsync(TestTenants.Primary, billId);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(BankSlipLine, raw, StringComparison.Ordinal);
    }

    // Importação manual nasce só com os dígitos — não há arquivo, e isso não é falha.
    [Fact]
    public async Task GetArtifact_WhenTheBillWasImportedByHand_ShouldReturnNotFound()
    {
        var billId = await SeedBillAsync(TenantA, withArtifact: false);

        var response = await GetAsync(TestTenants.Primary, billId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Boleto do outro tenant não abre — quem barra é o filtro por TenantId da query.
    [Fact]
    public async Task GetArtifact_WhenTheBillBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var billId = await SeedBillAsync(TenantB, withArtifact: true);

        var response = await GetAsync(TestTenants.Primary, billId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<BillId> SeedBillAsync(TenantId tenantId, bool withArtifact)
    {
        var key = withArtifact
            ? await _storage.StoreAsync(tenantId, "boleto.pdf", "application/pdf", Bytes, default)
            : null;

        return await ExecuteDbContextAsync(async db =>
        {
            var origin = withArtifact
                ? BillOrigin.Create(
                    BillSourceKind.Mailbox,
                    OccurredAt,
                    sourceId: new Guid("0195a1f0-0000-7000-8000-0000000000b9"),
                    senderAddress: "faturas@enel.com.br",
                    storageKey: key)
                : BillOrigin.Create(BillSourceKind.ManualUpload, OccurredAt, contentHash: "sha256:abc");

            var bill = Bill.Capture(
                tenantId,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(BankSlipLine, OccurredAt))],
                origin,
                OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id;
        });
    }

    private Task<HttpResponseMessage> GetAsync(Guid tenantId, BillId id)
        => _host.CreateClient().Authenticated().GetAsync(
            new Uri($"/api/v1/{tenantId}/bills/{id.Value}/artifact", UriKind.Relative));
}
